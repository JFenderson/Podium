using DocumentManagement.Core.Entities;
using DocumentManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ILogger<DocumentsController> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var documents = await _unitOfWork.Documents.FindAsync(d => d.UploadedBy == userId);

        var pagedDocuments = documents
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                id = d.Id,
                title = d.Title,
                description = d.Description,
                fileName = d.FileName,
                fileExtension = d.FileExtension,
                fileSizeInBytes = d.FileSizeInBytes,
                contentType = d.ContentType,
                uploadedAt = d.UploadedAt,
                modifiedAt = d.ModifiedAt,
                status = d.Status.ToString(),
                version = d.Version
            })
            .ToList();

        var totalCount = documents.Count();

        return Ok(new
        {
            data = pagedDocuments,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var document = await _unitOfWork.Documents.GetByIdAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        if (document.UploadedBy != userId && !document.IsPublic)
        {
            return Forbid();
        }

        return Ok(new
        {
            id = document.Id,
            title = document.Title,
            description = document.Description,
            fileName = document.FileName,
            fileExtension = document.FileExtension,
            fileSizeInBytes = document.FileSizeInBytes,
            contentType = document.ContentType,
            uploadedAt = document.UploadedAt,
            modifiedAt = document.ModifiedAt,
            status = document.Status.ToString(),
            version = document.Version,
            isPublic = document.IsPublic
        });
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        try
        {
            // Generate unique file name
            var fileExtension = Path.GetExtension(request.File.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Upload to storage
            string storagePath;
            using (var stream = request.File.OpenReadStream())
            {
                storagePath = await _storageService.UploadFileAsync(
                    stream,
                    uniqueFileName,
                    request.File.ContentType);
            }

            // Create document record
            var document = new Document
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                FileName = request.File.FileName,
                FileExtension = fileExtension,
                FileSizeInBytes = request.File.Length,
                StoragePath = storagePath,
                ContentType = request.File.ContentType,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                IsPublic = request.IsPublic
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
            {
                id = document.Id,
                title = document.Title,
                fileName = document.FileName,
                uploadedAt = document.UploadedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "An error occurred while uploading the document" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(int id, [FromBody] DocumentUpdateRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var document = await _unitOfWork.Documents.GetByIdAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        if (document.UploadedBy != userId)
        {
            return Forbid();
        }

        document.Title = request.Title ?? document.Title;
        document.Description = request.Description ?? document.Description;
        document.IsPublic = request.IsPublic ?? document.IsPublic;
        document.ModifiedAt = DateTime.UtcNow;
        document.ModifiedBy = userId;

        _unitOfWork.Documents.Update(document);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            id = document.Id,
            title = document.Title,
            description = document.Description,
            modifiedAt = document.ModifiedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var document = await _unitOfWork.Documents.GetByIdAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        if (document.UploadedBy != userId)
        {
            return Forbid();
        }

        try
        {
            // Soft delete
            document.IsDeleted = true;
            document.Status = DocumentStatus.Deleted;
            _unitOfWork.Documents.Update(document);
            await _unitOfWork.SaveChangesAsync();

            // Optionally delete from storage
            // await _storageService.DeleteFileAsync(document.StoragePath);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return StatusCode(500, new { error = "An error occurred while deleting the document" });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var document = await _unitOfWork.Documents.GetByIdAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        if (document.UploadedBy != userId && !document.IsPublic)
        {
            return Forbid();
        }

        try
        {
            var fileStream = await _storageService.DownloadFileAsync(document.StoragePath);
            return File(fileStream, document.ContentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document");
            return StatusCode(500, new { error = "An error occurred while downloading the document" });
        }
    }
}

public class DocumentUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
}

public class DocumentUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}