using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webboard.Api.Dtos;
using Webboard.Api.Security;
using Webboard.Api.Services;

namespace Webboard.Api.Controllers;

[ApiController]
[Route("api/topics/{topicId:int}/comments")]
[Authorize]
public class CommentsController(CommentService comments) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(int topicId, CreateCommentRequest request)
    {
        var result = await comments.CreateAsync(User.GetUserId(), topicId, request);
        return result.Status switch
        {
            // There is no single-comment endpoint; the comment is read as part of its topic.
            CommentStatus.Created => Created($"/api/topics/{topicId}", result.Comment),
            CommentStatus.TopicNotFound => Problem(statusCode: StatusCodes.Status404NotFound, detail: "Topic not found."),
            _ => Unauthorized(),
        };
    }
}
