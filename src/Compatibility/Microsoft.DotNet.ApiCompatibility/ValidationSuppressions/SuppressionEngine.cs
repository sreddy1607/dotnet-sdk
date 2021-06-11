﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ValidationSuppression
{
    /// <summary>
    /// Collection of Suppressions which is able to add suppressions, check if a specific error is suppressed, and write all suppressions
    /// down to a file. The engine is thread-safe.
    /// </summary>
    public class SuppressionEngine
    {
        protected HashSet<Suppression> _validationSuppressions;
        private ReaderWriterLockSlim _readerWriterLock = new();

        protected SuppressionEngine(string validationSuppressionFile)
        {
            _validationSuppressions = ParseValidationSuppressionBaseline(validationSuppressionFile);
        }

        protected SuppressionEngine()
        {
            _validationSuppressions = new HashSet<Suppression>();
        }

        /// <summary>
        /// Checks if the passed in error is suppressed or not.
        /// </summary>
        /// <param name="diagnosticId">The diagnostic ID of the error to check.</param>
        /// <param name="target">The target of where the <paramref name="diagnosticId"/> should be applied.</param>
        /// <param name="left">Optional. The left operand in a APICompat error.</param>
        /// <param name="right">Optional. The right operand in a APICompat error.</param>
        /// <returns><see langword="true"/> if the error is already suppressed. <see langword="false"/> otherwise.</returns>
        public bool IsErrorSuppressed(string? diagnosticId, string? target, string? left = null, string? right = null)
        {
            var suppressionToCheck = new Suppression()
            {
                DiagnosticId = diagnosticId,
                Target = target,
                Left = left,
                Right = right
            };
            return IsErrorSuppressed(suppressionToCheck);
        }

        /// <summary>
        /// Checks if the passed in error is suppressed or not.
        /// </summary>
        /// <param name="error">The <see cref="Suppression"/> error to check.</param>
        /// <returns><see langword="true"/> if the error is already suppressed. <see langword="false"/> otherwise.</returns>
        public bool IsErrorSuppressed(Suppression error)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if(_validationSuppressions.Contains(error))
                {
                    return true;
                }
                else
                {
                    // See if the error is globally suppressed by checking if the same diagnosticid and target are entered
                    // without any left and right.
                    if ((error.DiagnosticId == null || !error.DiagnosticId.StartsWith("pkv", StringComparison.InvariantCultureIgnoreCase)) 
                        && _validationSuppressions.Contains(new Suppression { DiagnosticId = error.DiagnosticId, Target = error.Target }))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds a suppression to the collection.
        /// </summary>
        /// <param name="diagnosticId">The diagnostic ID of the error to add.</param>
        /// <param name="target">The target of where the <paramref name="diagnosticId"/> should be applied.</param>
        /// <param name="left">Optional. The left operand in a APICompat error.</param>
        /// <param name="right">Optional. The right operand in a APICompat error.</param>
        public void AddSuppression(string? diagnosticId, string? target, string? left = null, string? right = null)
        {
            var suppressionToAdd = new Suppression()
            {
                DiagnosticId = diagnosticId,
                Target = target,
                Left = left,
                Right = right
            };
            AddSuppression(suppressionToAdd);
        }

        /// <summary>
        /// Adds a suppression to the collection.
        /// </summary>
        /// <param name="suppressionToAdd">The <see cref="Suppression"/> to be added.</param>
        public void AddSuppression(Suppression suppressionToAdd)
        {
            _readerWriterLock.EnterUpgradeableReadLock();
            try
            {
                if (!_validationSuppressions.Contains(suppressionToAdd))
                {
                    _readerWriterLock.EnterWriteLock();
                    try
                    {
                        _validationSuppressions.Add(suppressionToAdd);
                    }
                    finally
                    {
                        _readerWriterLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _readerWriterLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Writes all suppressions in collection down to a file.
        /// </summary>
        /// <param name="validationSuppressionFile">The path to the file to be written.</param>
        public void WriteSuppressionsToFile(string validationSuppressionFile)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Suppression[]));

            using (Stream writer = GetWritableStream(validationSuppressionFile))
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    serializer.Serialize(writer, _validationSuppressions.ToArray());
                    AfterWrittingSuppressionsCallback(writer);
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        protected virtual void AfterWrittingSuppressionsCallback(Stream stream)
        {
            // Do nothing. Used for tests.
        }

        /// <summary>
        /// Creates a new instance of <see cref="SuppressionEngine"/> based on the contents of a given suppression file.
        /// </summary>
        /// <param name="validationSuppressionFile">The path to the suppressions file to be used for initialization.</param>
        /// <returns>An instance of <see cref="SuppressionEngine"/>.</returns>
        public static SuppressionEngine CreateFromSuppressionFile(string validationSuppressionFile)
            => new SuppressionEngine(validationSuppressionFile);

        /// <summary>
        /// Creates a new instance of <see cref="SuppressionEngine"/> which is empty.
        /// </summary>
        /// <returns>An instance of <see cref="SuppressionEngine"/>.</returns>
        public static SuppressionEngine Create()
            => new SuppressionEngine();

        private HashSet<Suppression> ParseValidationSuppressionBaseline(string baselineFile)
        {
            HashSet<Suppression> result;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Suppression[]));
                using (Stream reader = GetReadableStream(baselineFile))
                {
                    Suppression[]? deserializedSuppressions = serializer.Deserialize(reader) as Suppression[];
                    if (deserializedSuppressions == null)
                    {
                        result = new HashSet<Suppression>();
                    }
                    else
                    {
                        result = new HashSet<Suppression>(deserializedSuppressions);
                    }
                }
            }
            catch (Exception)
            {
                result = new HashSet<Suppression>();
            }
            return result;
        }

        protected virtual Stream GetReadableStream(string baselineFile) => new FileStream(baselineFile, FileMode.Open);

        protected virtual Stream GetWritableStream(string validationSuppressionFile) => new FileStream(validationSuppressionFile, FileMode.OpenOrCreate);
    }
}
