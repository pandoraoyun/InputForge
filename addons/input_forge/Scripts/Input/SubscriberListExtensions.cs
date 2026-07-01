using System;
using System.Collections.Generic;

namespace InputForge;

/// <summary>
/// Dispatch helpers for the per-type subscriber lists held by <see cref="InputMappingContext"/>.
/// Each <c>Invoke</c> walks a list with a plain indexed loop and is null/empty-safe, so the
/// dispatch path can call <c>list.Invoke(value)</c> directly instead of repeating a
/// "null-check, count-check, for loop" block per callback type.
///
/// The value is computed once by the caller and passed in, so it is shared across every
/// subscriber in the list rather than rebuilt per element.
/// </summary>
internal static class SubscriberListExtensions
{
    public static void Invoke<T>(this List<Action<T>> callbacks, T value)
    {
        if (callbacks == null) return;
        // Indexed loop: no enumerator allocation, no per-event type switching.
        for (int i = 0; i < callbacks.Count; i++)
            callbacks[i](value);
    }
}
