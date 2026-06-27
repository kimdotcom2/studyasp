using Microsoft.AspNetCore.Mvc;
using studyasp.Controllers.request;
using studyasp.Controllers.response;
using studyasp.Services;

namespace studyasp.Controllers;

[ApiController]
[Route("/api")]
public class TestController : ControllerBase
{

    private readonly BoardService _boardService;

    public TestController(BoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpPost]
    public async Task<StatusCodeResult> Post([FromBody] PostBoardRequest request)
    {
        
        await _boardService.PostBoard(request);

        return new StatusCodeResult(201);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardPostResponse>> Get(string id)
    {
        return await _boardService.GetBoard(id);
    }

    [HttpPut("{id}")]
    public async Task<StatusCodeResult> Put(string id, [FromBody] PostBoardRequest request)
    {
        
        await _boardService.PutBoard(id, request);

        return new StatusCodeResult(200);
    }

    [HttpDelete("{id}")]
    public async Task<StatusCodeResult> Delete(string id)
    {
        
        await _boardService.DeleteBoard(id);

        return new StatusCodeResult(201);
    }
    
    /* [HttpGet]
    public string StringTest()
    {
        return "Hello World";
    }

    [HttpGet("{id}")]
    public string GetId(string id)
    {
        return id;
    }

    [HttpGet("/api/{id}")]
    public JsonResult GetApiId([FromRoute] string id)
    {
        return new JsonResult(id);
    }

    [HttpPost("/api/id")]
    public JsonResult GetApiIdByJson([FromBody] TestRequest request)
    {
        return new JsonResult(request.Id);
    }

    [HttpGet("/status")]
    public StatusCodeResult GetStatus()
    {
        return new StatusCodeResult(200);
    }

    [HttpPost("/image-upload")]
    public IActionResult UploadFile(IFormFile file)
    {

        Console.WriteLine(file.FileName);

        return (IActionResult)file;
    } */

}

