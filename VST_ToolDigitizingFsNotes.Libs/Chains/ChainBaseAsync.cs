namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public interface IHandleChainAsync<TRequest> where TRequest : class
{
    Task HandleAsync(TRequest request);
    void SetNextAsync(IHandleChainAsync<TRequest> nextChain);
}

public abstract class HandleChainBaseAsync<TRequest> : IHandleChainAsync<TRequest> where TRequest : class
{
    protected IHandleChainAsync<TRequest>? _nextChain;
    public void SetNextAsync(IHandleChainAsync<TRequest> nextChain)
    {
        _nextChain = nextChain;
    }
    public abstract Task HandleAsync(TRequest request);
}
