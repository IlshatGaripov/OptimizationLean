using System;
using System.Collections;
using System.Collections.Generic;

namespace Optimization.Genetic
{
    /// <summary>
    /// Comparer for Gene[] array
    /// </summary>
    public class GeneArrayComparer : IEqualityComparer<Gene[]>
    {
        public bool Equals(Gene[] x, Gene[] y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException();

            if (x.Length != y.Length)
            {
                Console.WriteLine("Gene arrays are not of equal size");
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                // If any of genes are not equal then false
                if (!CompareGenes(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;  // otherwise true
        }

        public int GetHashCode(Gene[] obj)
        {
            return string.Join(",", obj).GetHashCode();
        }

        static bool CompareGenes(Gene x, Gene y)
        {
            var areObjectsEqual = true;

            var xType = x.Value.GetType();
            var yType = y.Value.GetType();
            if (xType != yType)
            {
                Console.WriteLine("Gene types are not equal");
                return false;
            }

            // if the objects are primitive types such as (integer, string etc)
            // that implement IComparable, we can just directly try and compare the value     
            if (IsAssignableFrom(xType) || IsPrimitiveType(xType) || IsValueType(xType))
            {
                //compare the values
                if (!CompareValues(x.Value, y.Value))
                {
                    areObjectsEqual = false;
                }
            }
            //if the property is a collection (or something that implements IEnumerable)
            //we have to iterate through all items and compare values
            else if (IsEnumerableType(xType))
            {
                throw new NotImplementedException("No appropriate method implemented");
            }
            //if it is a class object, call the same function recursively again
            else if (xType.IsClass)
            {
                throw new NotImplementedException("No appropriate method implemented");
            }
            else
            {
                areObjectsEqual = false;
            }

            return areObjectsEqual;
        }

        //true if c and the current Type represent the same type, or if the current Type is in the inheritance
        //hierarchy of c, or if the current Type is an interface that c implements,
        //or if c is a generic type parameter and the current Type represents one of the constraints of c. false if none of these conditions are true, or if c is null.
        private static bool IsAssignableFrom(Type type)
        {
            return typeof(IComparable).IsAssignableFrom(type);
        }

        private static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive;
        }

        private static bool IsValueType(Type type)
        {
            return type.IsValueType;
        }

        private static bool IsEnumerableType(Type type)
        {
            return (typeof(IEnumerable).IsAssignableFrom(type));
        }

        /// <summary>
        /// Compares two values and returns if they are the same.
        /// </summary>       
        private static bool CompareValues(object value1, object value2)
        {
            bool areValuesEqual = true;
            IComparable selfValueComparer = value1 as IComparable;

            // one of the values is null            
            if (value1 == null && value2 != null || value1 != null && value2 == null)
                areValuesEqual = false;
            else if (selfValueComparer != null && selfValueComparer.CompareTo(value2) != 0)
                areValuesEqual = false;
            else if (!object.Equals(value1, value2))
                areValuesEqual = false;

            return areValuesEqual;
        }
    }
}
