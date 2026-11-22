namespace web.Services;

public interface IModelTrainingApiClient
{
    Task<PythonHealthDto> GetHealthAsync(
        CancellationToken cancellationToken = default);

    Task<RetrainStatusDto> GetRetrainStatusAsync(
        CancellationToken cancellationToken = default);

    Task<ModelRegistryResponseDto> GetModelsAsync(
        CancellationToken cancellationToken = default);

    Task StartRetrainAsync(
        byte[] csvBytes,
        string fileName,
        CancellationToken cancellationToken = default);

    Task SelectModelAsync(
        string version,
        CancellationToken cancellationToken = default);
}