﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Humanizer;

using Json.Schema;

using Microsoft.Teams.AI.Messages;

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    public ChatPrompt<TOptions> Function(IFunction function)
    {
        Functions.Add(function);
        Logger.Debug($"registered function '{function.Name}'", function.ToString());
        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, Delegate handler)
    {
        var func = new Function(name, description, handler);
        Functions.Add(func);
        Logger.Debug($"registered function '{func.Name}'", func.ToString());
        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, JsonSchema parameters, Delegate handler)
    {
        var func = new Function(name, description, parameters, handler);
        Functions.Add(func);
        Logger.Debug($"registered function '{func.Name}'", func.ToString());
        return this;
    }

    public async Task<object?> Invoke(FunctionCall call, CancellationToken cancellationToken = default)
    {
        var function = Functions.Get(call.Name) ?? throw new NotImplementedException();
        var logger = Logger.Child($"Functions.{call.Name}");

        if (function is Function func)
        {
            foreach (var plugin in ChatPlugins)
            {
                call = await plugin.OnBeforeFunctionCall(this, func, call, cancellationToken);
            }

            var startedAt = DateTime.Now;
            logger.Debug(call.Arguments);

            var res = await func.Invoke(call);
            var endedAt = DateTime.Now;

            logger.Debug(res);
            logger.Debug($"elapse time: {(endedAt - startedAt).Humanize(3)}");

            foreach (var plugin in ChatPlugins)
            {
                res = await plugin.OnAfterFunctionCall(this, func, call, res, cancellationToken);
            }

            return res;
        }

        return Task.FromResult<object?>(null);
    }
}