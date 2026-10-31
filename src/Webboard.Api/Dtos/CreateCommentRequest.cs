using System.ComponentModel.DataAnnotations;

namespace Webboard.Api.Dtos;

public record CreateCommentRequest([Required] string Body);
