using BuildingBlocks.CQRS;
using BuildingBlocks.Exceptions;
using Transpiler.API.Infrastructure;
using Transpiler.Core;

namespace Transpiler.API.Workflows.GenerateProject;

public sealed record GenerateProjectCommand(string WorkflowId) : ICommand<GenerateProjectResult>;

public sealed record GenerateProjectResult(string WorkflowId, IReadOnlyList<string> FilePaths, IReadOnlyList<string> Warnings);

internal sealed class GenerateProjectCommandHandler(IWorkflowCodeGenerator generator, IWorkflowSessionStore store)
    : ICommandHandler<GenerateProjectCommand, GenerateProjectResult>
{
    public Task<GenerateProjectResult> Handle(GenerateProjectCommand command, CancellationToken cancellationToken)
    {
        var entry = store.GetGraph(command.WorkflowId)
                    ?? throw new NotFoundException(nameof(Core.Model.WorkflowGraph), command.WorkflowId);

        var project = generator.Generate(entry.Graph, new CodeGenOptions());
        store.SaveGeneratedProject(command.WorkflowId, project);

        return Task.FromResult(new GenerateProjectResult(
            command.WorkflowId,
            project.Files.Select(f => f.RelativePath).ToList(),
            project.Warnings));
    }
}
