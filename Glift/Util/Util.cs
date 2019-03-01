using System;
using System.Collections.Generic;
using System.Collections;

namespace Util {
   public static class Test {
      public static void Assert(bool test, string msg = "") {
         if (!test)
            throw new Exception(msg);
      }
   }

   internal static class GenericElem {
      public static void AddToString<T>(
         ref string s, ref bool first, T item) {
         if (s == null)
            s = "";
         if (first) {
            s += $"{item.ToString()}";
            first = false;
         }
         else
            s += $",{item.ToString()}";
      }
   }

   public static class EnumerableExt {
      public static string ToString<T>(this IEnumerable seq) {
         string s = "";
         bool first = true;
         foreach (T item in seq)
            GenericElem.AddToString(ref s, ref first, item);
         return s;
      }
   }

   public static class ArrayExt {
      public static string ToString<T>(this T[] a) {
         return EnumerableExt.ToString<T>(a);
      }
   }

   public static class ListExt {
      public static string ToString<T>(this List<T> l) {
         return EnumerableExt.ToString<T>(l);
      }
   }
}
