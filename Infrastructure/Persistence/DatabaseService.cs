using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Postgrest.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class DatabaseService(
    Supabase.Client client,
    AuthenticationStateProvider CustomAuthStateProvider,
    ILocalStorageService localStorage,
    ILogger<DatabaseService> logger)
{
	private readonly Supabase.Client client = client;
	private readonly AuthenticationStateProvider customAuthStateProvider = CustomAuthStateProvider;
	private readonly ILocalStorageService localStorage = localStorage;
	private readonly ILogger<DatabaseService> logger = logger;

    public async Task<IReadOnlyList<TModel>> From<TModel>() where TModel : BaseModel, new()
	{
		Supabase.Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Get();
		return modeledResponse.Models;
	}
    public async Task<IReadOnlyList<TModel>> From<TModel>(Func<object, object> buildQuery) where TModel : BaseModel, new()
    {
        dynamic q = client.From<TModel>();
        q = buildQuery(q);
        var resp = await ((dynamic)q).Get();
        return resp.Models;
    }

    public async Task<List<TModel>> Delete<TModel>(TModel item) where TModel : BaseModel, new()
	{
		Supabase.Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Delete(item);
		return modeledResponse.Models;
	}

	public async Task<List<TModel>> Insert<TModel>(TModel item) where TModel : BaseModel, new()
	{
		Supabase.Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Insert(item);
		return modeledResponse.Models;
	}

	public async Task<List<TModel>> Update<TModel>(TModel item) where TModel: BaseModel, new()
	{
		var modeledResponse = await client.From<TModel>().Update(item);
		return modeledResponse.Models;
	}

    public async Task<TModel?> Single<TModel>(Func<object, object> buildQuery) where TModel : BaseModel, new()
	{
		dynamic q = client.From<TModel>();
		q = buildQuery(q);
		return await ((dynamic)q).Single();
	}

}
