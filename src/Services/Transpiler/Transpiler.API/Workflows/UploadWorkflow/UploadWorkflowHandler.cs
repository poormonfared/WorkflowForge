using BuildingBlocks.CQRS;
using FluentValidation;
using Transpiler.API.Infrastructure;
using Transpiler.Core;

namespace Transpiler.API.Workflows.UploadWorkflow;

public sealed record UploadWorkflowCommand(string RawSourceJson) : ICommand<UploadWorkflowResult>;

public sealed record UploadWorkflowResult(string WorkflowId, string WorkflowName, IReadOnlyList<string> Diagnostics);

public sealed class UploadWorkflowCommandValidator : AbstractValidator<UploadWorkflowCommand>
{
    public UploadWorkflowCommandValidator()
    {
        RuleFor(x => x.RawSourceJson).NotEmpty().WithMessage("Workflow JSON body is required");
    }
}

internal sealed class UploadWorkflowCommandHandler(IWorkflowSourceParser parser, IWorkflowSessionStore store)
    : ICommandHandler<UploadWorkflowCommand, UploadWorkflowResult>
{
    public Task<UploadWorkflowResult> Handle(UploadWorkflowCommand command, CancellationToken cancellationToken)
    {
        var graph = parser.Parse(command.RawSourceJson, new ParseOptions());
        var workflowId = store.SaveGraph(graph, parser.Diagnostics);

        return Task.FromResult(new UploadWorkflowResult(
            workflowId,
            graph.Name,
            parser.Diagnostics.Select(d => d.Message).ToList()));
    }
}
