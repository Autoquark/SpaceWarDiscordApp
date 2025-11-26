using System;

namespace SpaceWarDiscordApp;

public static class ObjectExtensions
{
    public static TResult? OrDefault<TIn, TResult>(this TIn? obj, Func<TIn, TResult> selector) where TIn : struct =>
        obj.OrDefault(selector, default);
    public static TResult OrDefault<TIn, TResult>(this TIn? obj, Func<TIn, TResult> selector, TResult defaultValue) where TIn : struct =>
        obj is null ? defaultValue : selector(obj.Value);
    
    public static TResult? OrDefault<TIn, TResult>(this TIn? obj, Func<TIn, TResult> selector) where TIn : class =>
        obj.OrDefault(selector, default);
    
    public static TResult OrDefault<TIn, TResult>(this TIn? obj, Func<TIn, TResult> selector, TResult defaultValue) where TIn : class =>
        obj is null ? defaultValue : selector(obj);
}