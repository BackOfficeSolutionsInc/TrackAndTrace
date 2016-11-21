﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public class HashUtil {

		public static int Merge(params int[] hashCodes) {
			
			int hash1 = (5381 << 16) + 5381;
			int hash2 = hash1;

			int i = 0;

			foreach (var hashCode in hashCodes) {
				if (i % 2 == 0)
					hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
				else
					hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

				++i;
			}

			return hash1 + (hash2 * 1566083941);

		}

	}
}