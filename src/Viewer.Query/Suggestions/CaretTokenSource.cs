﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Viewer.Query.Suggestions
{
    /// <summary>
    /// Caret token source returns all tokens up to a certain point (the caret position). After
    /// this point, it returns <see cref="CaretToken"/> instead.
    /// </summary>
    internal class CaretTokenSource : ITokenSource
    {
        private readonly Lexer _lexer;
        private readonly int _caretPosition;
        private readonly List<int> _separatorTokens;

        /// <summary>
        /// Create a new token source which will read tokens using <paramref name="lexer"/>.
        /// </summary>
        /// <param name="lexer">Lexer from which the tokens wil be read</param>
        /// <param name="caretPosition">Character index of the caret</param>
        /// <param name="separatorTokens">
        /// <para>
        /// Token types for which if the caret is right after them, they will be interpreted as
        /// 2 independent tokens. Otherwise, the original token will be replaced with CARET
        /// having the original token as ParentToken.
        /// </para>
        ///
        /// <para>
        /// <c>|</c> represents caret position in following examples. If ID is in the list,
        /// <c>abcd|</c> will be read as 2 tokens: ID(abcd), CARET. Otherwise, the source will
        /// return 1 token: CARET which will have ID(abcd) as its parent.
        /// </para>
        /// </param>
        public CaretTokenSource(Lexer lexer, int caretPosition, IEnumerable<int> separatorTokens)
        {
            _lexer = lexer;
            _caretPosition = caretPosition;
            _separatorTokens = separatorTokens.ToList();
        }

        private bool _caretTokenEmitted;

        public IToken NextToken()
        {
            var token = _lexer.NextToken();

            Trace.Assert(token.StartIndex >= 0);

            // emit caret token
            if (!_caretTokenEmitted)
            {
                int separateCaret = _separatorTokens.Contains(token.Type) ? 0 : 1;
                if (token.Type != Lexer.Eof &&
                    token.StartIndex <= _caretPosition &&
                    token.StopIndex + separateCaret >= _caretPosition)
                {
                    _caretTokenEmitted = true;
                    return new CaretToken(_lexer, InputStream, _caretPosition, token);
                }
                else if (token.Type == Lexer.Eof || token.StartIndex > _caretPosition)
                {
                    _caretTokenEmitted = true;
                    return new CaretToken(_lexer, InputStream, _caretPosition);
                }
            }

            // emit EOF token after the caret
            if (_caretTokenEmitted)
            {
                return new CommonToken(Lexer.Eof);
            }
            return token;
        }

        public int Line => _lexer.Line;
        public int Column => _lexer.Column;
        public ICharStream InputStream => ((ITokenSource)_lexer).InputStream;
        public string SourceName => _lexer.SourceName;
        public ITokenFactory TokenFactory
        {
            get => _lexer.TokenFactory;
            set => _lexer.TokenFactory = value;
        }
    }

}