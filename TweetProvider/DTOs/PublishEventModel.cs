using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TweetProvider.DTOs
{
    public class PublishEventModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JobId { get; set; }

        [Required]
        public string JobName { get; set; }

        public string JobGroup { get; set; }

        public string TopicName { get; set; }

        public string PubsubName { get; set; }

        public string EventPayload { get; set; }
        public int State { get; set; }
    }
}
