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
        /// Parse an entire YAML document as an instance of the given non-generic type.
        /// Note that this method is not suitable to parse parts of a YAML document
        /// because it throws an exception if the end of the stream has not been reached after parsing an instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type to parse the YAML document as. This must not be a generic type.</typeparam>
        /// <param name="reader">A reader over the YAML data.</param>
        /// <returns>The parsed instance.</returns>
        public static T ParseDocumentAsNonGeneric<T>(TextReader reader)
        {
            var parser = new Parser(reader);
            SkipDocumentStart(parser);

            var result = NonGenericFromYaml<T>(parser);

            CheckDocumentEnd(parser);
            return result;
        }

        /// <summary>
        /// Parse an entire YAML document as a list with the given (non-generic) element type.
        /// Note that this method is not suitable to parse parts of a YAML document
        /// because it throws an exception if the end of the stream has not been reached after parsing an instance of the given type.
        /// </summary>
        /// <typeparam name="TElement">The type of the list elements. This must not be a generic type.</typeparam>
        /// <param name="reader">A reader over the YAML data.</param>
        /// <returns>The parsed list.</returns>
        public static List<TElement> ParseDocumentAsList<TElement>(TextReader reader)
        {
            var parser = new Parser(reader);
            SkipDocumentStart(parser);

            var result = ListFromYaml<TElement>(parser);

            CheckDocumentEnd(parser);
            return result;
        }

        /// <summary>
        /// Parse an entire YAML document as a dictionary.
        /// Note that this method is not suitable to parse parts of a YAML document
        /// because it throws an exception if the end of the stream has not been reached after parsing an instance of the given type.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys. This must not be a generic type.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values. This must not be a generic type.</typeparam>
        /// <param name="reader">A reader over the YAML data.</param>
        /// <returns>The parsed list.</returns>
        public static Dictionary<TKey, TValue> ParseDocumentAsDictionary<TKey, TValue>(TextReader reader)
        {
            var parser = new Parser(reader);
            SkipDocumentStart(parser);

            var result = new Dictionary<TKey, TValue>(); // TODO DictionaryFromYaml<TKey, TValue>(parser);

            CheckDocumentEnd(parser);
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

            MoveNextAndCheckNotStreamEnd(parser);

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
        /// <typeparam name="TElement">The type of the list elements.</typeparam>
        /// <param name="parser">The current YAML parser.</param>
        /// <returns>The parsed list.</returns>
        internal static List<TElement> ListFromYaml<TElement>(IParser parser)
        {
            return ListFromYaml<TElement>(parser, typeof(TElement));
        }

        private static List<TElement> ListFromYaml<TElement>(IParser parser, Type elementType)
        {
            if (!(parser.Current is SequenceStart))
            {
                throw new YamlParseException($"Expected start of sequence at {parser.Current.Start}.");
            }

            MoveNextAndCheckNotStreamEnd(parser, "sequence item or end of sequence");

            List<TElement> list = new List<TElement>();
            while (!(parser.Current is SequenceEnd))
            {
                list.Add((TElement)NonGenericFromYaml(parser, elementType));
                MoveNextAndCheckNotStreamEnd(parser, "sequence item or end of sequence");
            }

            MoveNextAndCheckNotStreamEnd(parser);

            return list;
        }

        private static T NonGenericFromYaml<T>(IParser parser)
        {
            return (T)NonGenericFromYaml(parser, typeof(T));
        }

        private static object NonGenericFromYaml(IParser parser, Type type)
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

            // TODO maybe we can make the generics work if we use reflection to construct the List or Dictionary object that we want to return, just to make sure it has the right element type
            //else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            //{
            //    var typeArgs = type.GetGenericArguments();
            //    return ListFromYaml<object>(parser, typeArgs[0]);
            //}
            // TODO dictionary

            object result;
            ConsumeMappingStart(parser);
            try
            {
                var method = type.GetMethod("FromYaml");
                result = method.Invoke(null, new object[] { parser });
            }
            catch (Exception e)
            {
                if (e is YamlParseException)
                {
                    throw;
                }

                throw new YamlParseException($"Type {type} does not have a static 'FromYaml' method.", e);
            }

            ConsumeMappingEnd(parser);
            return result;
        }

        private static void SkipDocumentStart(IParser parser)
        {
            // This is necessary to make the parser start looking at the input.
            MoveNextAndCheckNotStreamEnd(parser, "start of stream or document");

            while (parser.Current is StreamStart || parser.Current is DocumentStart)
            {
                MoveNextAndCheckNotStreamEnd(parser);
            }
        }

        private static void CheckDocumentEnd(IParser parser)
        {
            while (parser.MoveNext())
            {
                if (!(parser.Current is StreamEnd || parser.Current is DocumentEnd))
                {
                    throw new YamlParseException($"Expected end of document or stream at {parser.Current.Start}.");
                }
            }
        }

        private static void ConsumeMappingStart(IParser parser)
        {
            if (!(parser.Current is MappingStart))
            {
                throw new YamlParseException($"Expected start of mapping at {parser.Current.Start}.");
            }

            MoveNextAndCheckNotStreamEnd(parser, "mapping key or end of mapping");
        }

        private static void ConsumeMappingEnd(IParser parser)
        {
            if (!(parser.Current is MappingEnd))
            {
                throw new YamlParseException($"Expected end of mapping at {parser.Current.Start}.");
            }

            MoveNextAndCheckNotStreamEnd(parser);
        }

        private static void MoveNextAndCheckNotStreamEnd(IParser parser)
        {
            MoveNextAndCheckNotStreamEnd(parser, string.Empty);
        }

        private static void MoveNextAndCheckNotStreamEnd(IParser parser, string expectation)
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
