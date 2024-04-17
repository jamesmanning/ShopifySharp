﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ShopifySharp.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;
using ShopifySharp.Utilities;

namespace ShopifySharp;

/// <summary>
/// A service for using or manipulating Shopify's Graph API.
/// </summary>
public class GraphService : ShopifyService, IGraphService
{
    private readonly IGraphSerializer _graphSerializer;
    private readonly string _apiVersion;

    public override string APIVersion => _apiVersion ?? base.APIVersion;

    public GraphService(
        string myShopifyUrl,
        string shopAccessToken,
        string apiVersion = null,
        IGraphSerializer graphSerializer = null
    ) : base(myShopifyUrl, shopAccessToken)
    {
        _apiVersion = apiVersion;
        _graphSerializer = graphSerializer ?? new GraphSerializer();
    }

    public GraphService(
        string myShopifyUrl,
        string shopAccessToken,
        IShopifyDomainUtility shopifyDomainUtility,
        IGraphSerializer graphSerializer = null
    ) : base(myShopifyUrl, shopAccessToken, shopifyDomainUtility)
    {
        _graphSerializer = graphSerializer ?? new GraphSerializer();
    }

    public virtual async Task<T> PostAsync<T>(GraphRequest graphRequest, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<JsonDocument>(graphRequest, cancellationToken);
        // TODO: return a GraphResult<T>
        return response.RootElement.GetProperty("data").Deserialize<T>();
    }

    public virtual async Task<JsonDocument> PostAsync(GraphRequest graphRequest, CancellationToken cancellationToken = default)
    {
        return await PostAsync<JsonDocument>(graphRequest, cancellationToken);
    }

    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<JToken> PostAsync(JToken body, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<JToken>(new GraphRequest
        {
            Query = body.SelectToken("query").Value<string>(),
            Variables = body.SelectToken("variables").Value<Dictionary<string, object>>(),
            EstimatedQueryCost = graphqlQueryCost
        }, cancellationToken);

        return response.SelectToken("data");
    }

    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<JToken> PostAsync(string graphqlQuery, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<JToken>(new GraphRequest
        {
            Query = graphqlQuery,
            Variables = null,
            EstimatedQueryCost = graphqlQueryCost
        }, cancellationToken);

        return response.SelectToken("data");
    }

    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<JsonElement> SendAsync(string graphqlQuery, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<JsonDocument>(new GraphRequest 
        {
            Query = graphqlQuery,
            Variables = null,
            EstimatedQueryCost = graphqlQueryCost,
        }, cancellationToken);

        return response.RootElement.GetProperty("data");
    }

    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<JsonElement> SendAsync(GraphRequest request, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<JsonDocument>(new GraphRequest 
        {
            Query = request.Query,
            Variables = request.Variables,
            EstimatedQueryCost = graphqlQueryCost ?? request.EstimatedQueryCost,
        }, cancellationToken);

        return response.RootElement.GetProperty("data");
    }

#if NET6_0_OR_GREATER
    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<TResult> SendAsync<TResult>(string graphqlQuery, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
        where TResult : class
    {
        return await SendAsync<TResult>(new GraphRequest 
        {
            Query = graphqlQuery,
            Variables = null,
            EstimatedQueryCost = graphqlQueryCost,
        }, cancellationToken);
    }

    /// <summary>
    /// Issue a single value query and return the value as an strongly typed object.
    /// Use a type from the ShopifySharp.GraphQL namespace
    /// </summary>
    /// <typeparam name="TResult">Use a type from the ShopifySharp.GraphQL namespace</typeparam>
    /// <param name="request"></param>
    /// <param name="graphqlQueryCost"></param>
    /// <param name="cancellationToken"></param>
    [Obsolete("This method is deprecated and will be removed in a future version of ShopifySharp.")]
    public virtual async Task<TResult> SendAsync<TResult>(GraphRequest request, int? graphqlQueryCost = null, CancellationToken cancellationToken = default)
        where TResult : class
    {
        // TODO: add a test for this line that's now been replaced. Are we losing any significant functionality by dropping the `.Single()`? Would this have failed for e.g. mutations that return both `userErrors` and `actualObject`?
        // var ptyElt = elt.EnumerateObject().Single().Value;

        return await SendAsync<TResult>(new GraphRequest 
        {
            Query = request.Query,
            Variables = request.Variables,
            EstimatedQueryCost = graphqlQueryCost ?? request.EstimatedQueryCost,
        }, cancellationToken);
    }
#endif

    /// <summary>
    /// Content agnostic way to send the request, regardless of Json or GraphQL.
    /// </summary>
    /// <param name="req">The RequestUri.</param>
    /// <param name="content">The HttpContent, be it GraphQL or Json.</param>
    /// <param name="graphqlQueryCost">An estimation of the cost of this query.</param>
    /// <param name="cancellationToken"></param>
    protected virtual async Task<T> SendAsync<T>(GraphRequest graphRequest, CancellationToken cancellationToken = default)
    {
        var json = _graphSerializer.SerializeToJson(new Dictionary<string, object>
        {
            {"query", graphRequest.Query},
            {"variables", graphRequest.Variables},
        });
        var requestUri = BuildRequestUri("graphql.json");
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await ExecuteRequestCoreAsync<T>(requestUri, HttpMethod.Post, cancellationToken, requestContent, null, null, graphRequest.EstimatedQueryCost, DateParseHandling.None);

        CheckForErrors(response);

        return response.Result;
    }

    /// <summary>
    /// Since Graph API Errors come back with error code 200, checking for them in a way similar to the REST API doesn't work well without potentially throwing unnecessary errors. This loses the requestId, but otherwise is capable of passing along the message.
    /// </summary>
    /// <param name="requestResult">The <see cref="RequestResult{JToken}" /> response from ExecuteRequestAsync.</param>
    /// <exception cref="ShopifyException">Thrown if <paramref name="requestResult"/> contains an error.</exception>
    protected virtual void CheckForErrors<T>(RequestResult<T> requestResult)
    {
        var parsedData = _graphSerializer.DeserializeFromJson<ParsedGraphResponse<T>>(requestResult.RawResult);

        if (parsedData?.UserErrors is null)
            return;

        var errorList = new List<string>();

        foreach (var error in parsedData.UserErrors)
        {
            if (error.message is not null)
            {
                errorList.Add(error.message);
            }
        }

        var message = errorList.FirstOrDefault() ?? "Unable to parse Shopify's error response, please inspect exception's RawBody property and report this issue to the ShopifySharp maintainers.";
        var requestId = ParseRequestIdResponseHeader(requestResult.ResponseHeaders);

        throw new ShopifyHttpException(requestResult.RequestInfo, HttpStatusCode.OK, errorList, message, requestResult.RawResult, requestId);
    }
}
