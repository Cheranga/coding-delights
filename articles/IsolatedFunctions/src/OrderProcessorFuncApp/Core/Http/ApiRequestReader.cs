using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Core.Http;

internal sealed class ApiRequestReader<TDto, TDtoValidator>(
    JsonSerializerOptions serializerOptions,
    IValidator<TDto> validator,
    ILogger<ApiRequestReader<TDto, TDtoValidator>> logger
) : IApiRequestReader<TDto, TDtoValidator>
    where TDto : class, IApiRequestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>
{
    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TDto>>> ReadRequestAsync(
        HttpRequestData request,
        CancellationToken token
    )
    {
        try
        {
            var dto = await JsonSerializer.DeserializeAsync<TDto>(request.Body, options: serializerOptions, cancellationToken: token);
            if (dto is null)
            {
                logger.LogError("Request body is null");
                return OperationResult.FailedResult.New(ErrorCodes.InvalidRequestSchema, ErrorMessages.InvalidRequestSchema);
            }

            var validationResult = await validator.ValidateAsync(dto, token);
            if (validationResult.IsValid)
            {
                return OperationResult.SuccessResult<TDto>.New(dto);
            }
            logger.LogError("Request body is invalid");
            return OperationResult.FailedResult.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error when reading request");
            return OperationResult.FailedResult.New(ErrorCodes.InvalidRequestSchema, ErrorMessages.InvalidRequestSchema);
        }
    }
}
