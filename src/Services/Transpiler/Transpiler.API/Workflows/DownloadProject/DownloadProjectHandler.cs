using System.IO.Compression;
using System.Text;
using BuildingBlocks.CQRS;
using BuildingBlocks.Exceptions;
using Transpiler.API.Infrastructure;
using Transpiler.Core;

namespace Transpiler.API.Workflows.DownloadProject;

public sealed record DownloadProjectQuery(string WorkflowId) : IQuery<byte[]>;

internal sealed class DownloadProjectQueryHandler(IWorkflowSessionStore store) : IQueryHandler<DownloadProjectQuery, byte[]>
{
    public Task<byte[]> Handle(DownloadProjectQuery query, CancellationToken cancellationToken)
    {
        var project = store.GetGeneratedProject(query.WorkflowId)
                      ?? throw new NotFoundException(nameof(GeneratedProject), query.WorkflowId);

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in project.Files)
            {
                var entry = zip.CreateEntry(file.RelativePath.Replace('\\', '/'));
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(file.Contents);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return Task.FromResult(stream.ToArray());
    }
}
