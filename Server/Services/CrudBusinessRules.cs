using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yosh.uwa.Models;

namespace Yosh.uwa.Extensibility
{
    public class CrudRuleContext
    {
        public CrudDefinition Definition { get; init; }
        public string Operation { get; init; }
        public string KeyValue { get; init; }
        public IDictionary<string, string> Values { get; init; }
    }

    public interface ICrudBusinessRule
    {
        string Key { get; }

        Task<CrudOperationResult> ValidateAsync(CrudRuleContext context, CancellationToken cancellationToken)
            => Task.FromResult(CrudOperationResult.Success());

        Task BeforeExecuteAsync(CrudRuleContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task AfterExecuteAsync(CrudRuleContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
