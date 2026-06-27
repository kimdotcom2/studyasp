using studyasp.Controllers.request;
using studyasp.Controllers.response;
using studyasp.Data;
using studyasp.Dtos;
using studyasp.Models;
using studyasp.Repositories;

namespace studyasp.Services;

public class BoardService
{
    
    private readonly AppDbContext _context;
    private readonly BoardRepository _repository;
    public BoardService(AppDbContext context, BoardRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task PostBoard(PostBoardRequest request)
    {

        Board newPost = Board.Of(request.Title, request.Content);

        _context.Boards.Add(newPost);
        
        await _context.SaveChangesAsync();

    }

    public async Task<BoardPostResponse> GetBoard(string id)
    {

        BoardView board = await _repository.GetBoard(id);
        // Todo
        return new BoardPostResponse(board.Id, board.Title, board.Content);
    }

    public async Task PutBoard(string id, PostBoardRequest request)
    {
        Board? board = await _context.Boards.FindAsync(id);
        board?.Update(request.Title, request.Content);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBoard(string id)
    {
        Board? board = await _context.Boards.FindAsync(id);
        _context.Boards.Remove(board!);
        await _context.SaveChangesAsync();
    }

}