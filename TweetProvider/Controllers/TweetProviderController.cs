﻿using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Quartz;
using TweetProvider.Jobs;
using Quartz.Impl.Matchers;
using Microsoft.EntityFrameworkCore;
using TweetProvider.DbContexts;
using TweetProvider.DTOs;
using Google.Api;

namespace TweetProvider.Controllers
{
    [ApiController]
    public class TweetProviderController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<TweetProviderController> _logger;
        private readonly PublishEventDbContext _dbContext;

        public TweetProviderController(DaprClient daprClient, PublishEventDbContext dbContext, ISchedulerFactory schedulerFactory, ILogger<TweetProviderController> logger)
        {
            _daprClient = daprClient;
            _dbContext = dbContext;
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        [HttpPost("/tweets")]
        public async Task<IActionResult> ProcessTweet(Tweet tweet)
        {
            var state = await _daprClient.GetStateEntryAsync<Tweet>("tweet-store", tweet.Id);
            state.Value = tweet;
            await state.SaveAsync();

            tweet.Id = Guid.NewGuid().ToString("N");

            await _daprClient.SaveStateAsync("tweet-store", tweet.Id, tweet);
            _logger.LogInformation($"Saved tweet with id {tweet.Id}");

            var scheduler = await _schedulerFactory.GetScheduler();
            var job = JobBuilder.Create<PublishEventJob>()
                .WithIdentity(tweet.Id, "tweets")
                .UsingJobData("TopicName", "tweets")
                .UsingJobData("PubSubName", "tweets-pubsub")
                .UsingJobData("EventPayload", JsonSerializer.Serialize(tweet))
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{tweet.Id}", "tweets")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(new Random().Next(10, 30)))
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            _dbContext.Add(new PublishEventModel
            {
                JobName = tweet.Id,
                JobGroup = "tweets",
                TopicName = "tweets",
                PubsubName = "tweets-pubsub",
                EventPayload = JsonSerializer.Serialize(tweet),
                State = 1
            });

            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("/cancel")]
        public async Task<IActionResult> ProcessCancel(string tweetId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(tweetId, "tweets");
            var jobDeleted = await scheduler.DeleteJob(jobKey);

            if (jobDeleted)
            {
                return Ok($"Job {tweetId} cancelled successfully.");
            }
            else
            {
                return NotFound($"Job {tweetId} not found.");
            }
        }

        [HttpGet("scheduler-jobs")]
        public async Task<IActionResult> GetGetSchedulerJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobGroupNames = await scheduler.GetJobGroupNames();

            var jobs = new List<object>();

            foreach (var group in jobGroupNames)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));

                foreach (var jobKey in jobKeys)
                {
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);

                    foreach (var trigger in triggers)
                    {
                        var triggerState = await scheduler.GetTriggerState(trigger.Key);
                        var nextFireTime = trigger.GetNextFireTimeUtc()?.ToLocalTime();
                        var previousFireTime = trigger.GetPreviousFireTimeUtc()?.ToLocalTime();

                        jobs.Add(new
                        {
                            JobName = jobKey.Name,
                            Group = jobKey.Group,
                            Trigger = trigger.Key.Name,
                            State = triggerState.ToString(),
                            NextFireTime = nextFireTime.HasValue ? nextFireTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A",
                            PreviousFireTime = previousFireTime.HasValue ? previousFireTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A"
                        });
                    }
                }
            }
            return Ok(jobs);
        }

        private string MapState(int state)
        {
            return state switch
            {
                0 => "Pending",
                1 => "Running",
                2 => "Completed",
                3 => "Failed",
                _ => "Unknown"
            };
        }

        [HttpGet("all-jobs")]
        public async Task<IActionResult> GetAllJobsWithStatus()
        {
            var jobs = await _dbContext.PublishEventJobs.ToListAsync();

            var result = jobs.Select(job => new
            {
                JobId = job.JobId,
                JobName = job.JobName,
                JobGroup = job.JobGroup,
                TopicName = job.TopicName,
                PubsubName = job.PubsubName,
                EventPayload = job.EventPayload,
                State = MapState(job.State)
            }).ToList();

            return Ok(result);
        }
    }
}
