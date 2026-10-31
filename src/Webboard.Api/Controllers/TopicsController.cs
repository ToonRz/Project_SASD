using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webboard.Api.Dtos;
using Webboard.Api.Security;
using Webboard.Api.Services;

namespace Webboard.Api.Controllers;

[ApiController]
[Route("api/topics")]
public class TopicsController(TopicService topics) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<TopicSummaryDto>> List() => await topics.ListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TopicDetailDto>> Get(int id)
    {
        var topic = await topics.GetAsync(id);
        return topic is null ? Problem(statusCode: StatusCodes.Status404NotFound, detail: "Topic not found.") : topic;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TopicSummaryDto>> Create(CreateTopicRequest request)
    {
        var topic = await topics.CreateAsync(User.GetUserId(), request);
        return topic is null
            ? Unauthorized()
            : Created($"/api/topics/{topic.Id}", topic);
    }
}
