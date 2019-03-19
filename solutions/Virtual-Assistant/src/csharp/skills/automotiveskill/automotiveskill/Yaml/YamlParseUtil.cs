// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SharpYaml;
using SharpYaml.Events;

namespace AutomotiveSkill.Yaml
{
    /// <summary>
    /// Utilities for YAML parsing.
    /// </summary>
    public class YamlParseUtil
    {
        private YamlParseUtil()
        {
        }

        /// <summary>
        /// Parse an entire YAML document as an instance of the given type.
        /// Note that this method is not suitable to parse parts of a YAML document
        /// because it throws an exception if the end of the stream has not been reached after parsing an instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type to parse the YAML document as.</typeparam>
        /// <param name="reader">A reader over the YAML data.</param>
        /// <returns>The parsed instance.</returns>
        public static T ParseDocument<T>(TextReader reader)
        {
            var parser = new Parser(reader);

            // This is necessary to make the parser start looking at the input.
            parser.MoveNext();

            while (parser.Current is StreamStart || parser.Current is DocumentStart)
            {
                MoveNext(parser);
            }

            var result = FromYaml<T>(parser);

            while (parser.MoveNext())
            {
                if (!(parser.Current is StreamEnd || parser.Current is DocumentEnd))
                {
                    throw new YamlParseException($"Expected end of stream at {parser.Current.Start}.");
                }
            }

            return result;
        }

        /// <summary>
        /// Parse a string value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static string StringFromYaml(IParser parser)
        {
            if (!(parser.Current is Scalar scalar))
            {
                throw new YamlParseException($"Expected scalar at {parser.Current.Start}.");
            }

            MoveNext(parser, "scalar");

            return scalar.Value;
        }

        /// <summary>
        /// Parse a boolean value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static bool BoolFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!bool.TryParse(str, out bool result))
            {
                throw new YamlParseException($"Failed to parse scalar as bool: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse an integer value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static int IntFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!int.TryParse(str, out int result))
            {
                throw new YamlParseException($"Failed to parse scalar as int: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a long value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static long LongFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!long.TryParse(str, out long result))
            {
                throw new YamlParseException($"Failed to parse scalar as long: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a double value.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static double DoubleFromYaml(IParser parser)
        {
            var str = StringFromYaml(parser);

            if (!double.TryParse(str, out double result))
            {
                throw new YamlParseException($"Failed to parse scalar as double: \"{str}\" at {parser.Current.Start}.");
            }

            return result;
        }

        /// <summary>
        /// Parse a list of values.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <typeparam name="T">The type of the list elements.</typeparam>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed list.</returns>
        internal static List<T> ListFromYaml<T>(IParser parser)
        {
            return (List<T>)(object)ListFromYaml(parser, typeof(T));
        }

        /// <summary>
        /// Parse a value of an arbitrary type.
        /// This method would typically be called from the 'FromYaml' method of a custom class.
        /// </summary>
        /// <typeparam name="T">The type of value to parse. Custom types must have a static 'FromYaml' method that accepts an IParser and returns an object of that type.</typeparam>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed value.</returns>
        internal static T FromYaml<T>(IParser parser)
        {
            return (T)FromYaml(parser, typeof(T));
        }

        private static List<object> ListFromYaml(IParser parser, Type elementType)
        {
            if (!(parser.Current is SequenceStart))
            {
                throw new YamlParseException($"Expected start of sequence at {parser.Current.Start}.");
            }

            MoveNext(parser, "sequence item or end of sequence");

            List<object> list = new List<object>();
            while (!(parser.Current is SequenceEnd))
            {
                list.Add(FromYaml(parser, elementType));
                MoveNext(parser, "sequence item or end of sequence");
            }

            MoveNext(parser);

            return list;
        }

        private static object FromYaml(IParser parser, Type type)
        {
            if (typeof(string) == type)
            {
                return StringFromYaml(parser);
            }
            else if (typeof(bool) == type)
            {
                return BoolFromYaml(parser);
            }
            else if (typeof(int) == type)
            {
                return IntFromYaml(parser);
            }
            else if (typeof(long) == type)
            {
                return LongFromYaml(parser);
            }
            else if (typeof(double) == type)
            {
                return DoubleFromYaml(parser);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var typeArgs = type.GetGenericArguments();
                return ListFromYaml(parser, typeArgs[0]);
            }

            try
            {
                var method = type.GetMethod("FromYaml");
                return method.Invoke(null, new object[] { parser });
            }
            catch (Exception e)
            {
                if (e is YamlParseException)
                {
                    throw;
                }

                throw new YamlParseException($"Type {type} does not have a static 'FromYaml' method.", e);
            }
        }

        private static void MoveNext(IParser parser)
        {
            MoveNext(parser, string.Empty);
        }

        private static void MoveNext(IParser parser, string expectation)
        {
            if (!parser.MoveNext() || parser.Current is StreamEnd || parser.Current is DocumentEnd)
            {
                string endOfWhat = "stream";
                if (parser.Current is DocumentEnd)
                {
                    endOfWhat = "document";
                }

                string expectationSentence = string.Empty;
                if (!string.IsNullOrEmpty(expectation))
                {
                    expectationSentence = $" Expected {expectation}.";
                }

                throw new YamlParseException($"Unexpected end of {endOfWhat} at {parser.Current.Start}.{expectationSentence}");
            }
        }
    }
}
