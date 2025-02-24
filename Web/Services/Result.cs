using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;

namespace Mictlanix.BE.Web.Services {
	public sealed class Unit {
		public static readonly Unit Value = new Unit ();
		private Unit () { }
	}

	public struct Result<T> {
		public readonly T Value;

		public static implicit operator Result<T> (T value) => new Result<T> (value);

		public readonly ImmutableArray <string> Errors;
		public bool Success => Errors.Length == 0;

		public Result (T value)
		{
			Value = value;
			Errors = ImmutableArray <string>.Empty;
		}

		public Result (ImmutableArray <string> errors)
		{
			if (errors.Length == 0) {
				throw new InvalidOperationException ("Error List Empty");
			}

			Value = default (T);
			Errors = errors;
		}
	}

	public static class Result {
		public static readonly Unit Unit = Unit.Value;

		public static Result<T> Success<T> (this T value) => new Result<T> (value);

		public static Result<T> Failure<T> (ImmutableArray<string> errors) => new Result<T> (errors);

		public static Result<T> Failure<T> (string error) => new Result<T> (ImmutableArray.Create (error));

		public static Result<Unit> Success () => new Result<Unit> (Unit);

		public static Result<Unit> Failure (ImmutableArray<string> errors) => new Result<Unit> (errors);

		public static Result<Unit> Failure (IEnumerable<string> errors) => new Result<Unit> (ImmutableArray.Create (errors.ToArray ()));

		public static Result<Unit> Failure (string error) => new Result<Unit> (ImmutableArray.Create (error));
	}
}