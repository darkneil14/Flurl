﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Flurl.Util;

namespace Flurl.Http.Testing
{
	/// <summary>
	/// Utility methods used by both HttpTestSetup and HttpTestAssertion
	/// </summary>
	internal static class Util
	{
		internal static bool HasAnyVerb(this FlurlCall call, HttpMethod[] verbs) {
			// for good measure, check both FlurlRequest.Verb and HttpRequestMessage.Method
			return verbs.Any(verb => call.Request.Verb == verb && call.HttpRequestMessage.Method == verb);
		}

		internal static bool HasAnyVerb(this FlurlCall call, string[] verbs) {
			// for good measure, check both FlurlRequest.Verb and HttpRequestMessage.Method
			return verbs.Any(verb =>
				call.Request.Verb.Method.Equals(verb, StringComparison.OrdinalIgnoreCase) &&
				call.HttpRequestMessage.Method.Method.Equals(verb, StringComparison.OrdinalIgnoreCase));
		}

		internal static bool HasQueryParam(this FlurlCall call, string name, object value) {
			var paramVals = call.Request.Url.QueryParams
				.Where(p => p.Name == name)
				.Select(p => p.Value.ToInvariantString())
				.ToList();

			if (!paramVals.Any())
				return false;
			if (!(value is string) && value is IEnumerable en) {
				var values = en.Cast<object>().Select(o => o.ToInvariantString()).ToList();
				return values.Intersect(paramVals).Count() == values.Count;
			}
			return paramVals.Any(v => MatchesValue(v, value));
		}

		internal static bool HasAllQueryParams(this FlurlCall call, string[] names) {
			return call.Request.Url.QueryParams
			   .Select(p => p.Name)
			   .Intersect(names)
			   .Count() == names.Length;
		}

		internal static bool HasAnyQueryParam(this FlurlCall call, string[] names) {
			var qp = call.Request.Url.QueryParams;
			return names.Any() ? qp
			   .Select(p => p.Name)
			   .Intersect(names)
			   .Any() : qp.Any();
		}

		internal static bool HasQueryParams(this FlurlCall call, object values) {
			return values.ToKeyValuePairs().All(kv => call.HasQueryParam(kv.Key, kv.Value));
		}

		internal static bool HasHeader(this FlurlCall call, string name, object value) {
			return call.Request.Headers.TryGetValue(name, out var val) && MatchesValue(val?.ToInvariantString(), value);
		}

		internal static bool HasCookie(this FlurlCall call, string name, object value) {
			return call.Request.Cookies.TryGetValue(name, out var val) && MatchesValue(val, value);
		}

		private static bool MatchesValue(string valueToMatch, object value) {
			if (value == null)
				return true;
			if (valueToMatch == null)
				return false;
			if (value is string s)
				return MatchesPattern(valueToMatch, s);
			return valueToMatch == value.ToInvariantString();
		}

		internal static bool MatchesPattern(string textToCheck, string pattern) {
			// avoid regex'ing in simple cases
			if (string.IsNullOrEmpty(pattern) || pattern == "*") return true;
			if (string.IsNullOrEmpty(textToCheck)) return false;
			if (!pattern.Contains("*")) return textToCheck == pattern;

			var regex = "^" + Regex.Escape(pattern).Replace("\\*", "(.*)") + "$";
			return Regex.IsMatch(textToCheck ?? "", regex);
		}
	}
}
