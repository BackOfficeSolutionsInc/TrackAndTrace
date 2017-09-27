﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Utilities {
	public static class AsyncHelper {
		private static readonly TaskFactory _myTaskFactory = new
		  TaskFactory(CancellationToken.None,
					  TaskCreationOptions.None,
					  TaskContinuationOptions.None,
					  TaskScheduler.Default);

		private class State {
			public HttpContext httpContext { get; set; }
			public State() {
				httpContext = HttpContext.Current;
			}

			public void Deconstruct() {
				HttpContext.Current = httpContext;
			}
		}

		public static TResult RunSync<TResult>(Func<Task<TResult>> func) {
			return AsyncHelper._myTaskFactory
			  .StartNew<Task<TResult>>((st) => {
				  ((State)st).Deconstruct();
				  return func();
			  }, new State())
			  .Unwrap<TResult>()
			  .GetAwaiter()
			  .GetResult();
		}

		public static void RunSync(Func<Task> func) {
			AsyncHelper._myTaskFactory
			  .StartNew<Task>((st) => {
				  ((State)st).Deconstruct();
				  return func();
			  }, new State())
			  .Unwrap()
			  .GetAwaiter()
			  .GetResult();
		}
	}
}