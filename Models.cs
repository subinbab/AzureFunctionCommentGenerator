using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocxoBlurbCommentGenerator
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public DateTimeOffset? DateDeleted { get; set; }
    }
    [Table("Blurb")]
    public class Blurbs : BaseEntity
    {
        public string Link { get; set; } // The link associated with the blurb
        public string Title { get; set; } // Title of the blurb (text)
        public string Description { get; set; } // Description of the blurb (text)
        public int Limit { get; set; } // Limit for something (e.g., access or views)
        public int RemainingLimit { get; set; } // The remaining available limit
    }
    public class ClientIds
    {
        public Guid id { get; set; }
        public string clientName { get; set; }
        public string clientId { get; set; }
    }
    public enum Clients
    {
        Socxo = 0,
        Socxly = 1
    }
    public class RequestModel
    {
        public string link { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string clientId { get; set; } = string.Empty;
        public int wordLimit { get; set; }
    }
    public class Requests : BaseEntity
    {
        public string ClientId { get; set; }
        public string Domian { get; set; }
        public DateTime RequestedTime { get; set; }
        public List<Blurbs> Blurb { get; set; }
        public int Limit { get; set; }
        public int RemainingLimit { get; set; }
    }
    public record Response(
    List<GeneratedComments> GeneratedComments,
    string Link,
    bool IsSuccess,
    string ErrorMessage,
    int RemainingBlurbRequest,
    int RemainingClientRequest,
    string Resource,
    string RequestedClient,
    string BlurbRequestLimitResetingIn,
    string ClientRequestLimitResetingIn
);

    public class GeneratedComments
    {
        public string id { get; set; }
        public string generatedComment { get; set; }
    }
}
