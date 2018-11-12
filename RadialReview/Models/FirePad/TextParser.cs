using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class TextParser {
		private string _text;
		private int _pos;

		public string Text { get { return _text; } }
		public int Position { get { return _pos; } }
		public int Remaining { get { return _text.Length - _pos; } }
		public static char NullChar = (char)0;

		public TextParser() {
			Reset(null);
		}

		public TextParser(string text) {
			Reset(text);
		}

		/// <summary>
		/// Resets the current position to the start of the current document
		/// </summary>
		public void Reset() {
			_pos = 0;
		}

		/// <summary>
		/// Sets the current document and resets the current position to the start of it
		/// </summary>
		/// <param name="html"></param>
		public void Reset(string text) {
			_text = (text != null) ? text : String.Empty;
			_pos = 0;
		}

		/// <summary>
		/// Indicates if the current position is at the end of the current document
		/// </summary>
		public bool EndOfText {
			get { return (_pos >= _text.Length); }
		}

		/// <summary>
		/// Returns the character at the current position, or a null character if we're
		/// at the end of the document
		/// </summary>
		/// <returns>The character at the current position</returns>
		public char Peek() {
			return Peek(0);
		}

		/// <summary>
		/// Returns the character at the specified number of characters beyond the current
		/// position, or a null character if the specified position is at the end of the
		/// document
		/// </summary>
		/// <param name="ahead">The number of characters beyond the current position</param>
		/// <returns>The character at the specified position</returns>
		public char Peek(int ahead) {
			int pos = (_pos + ahead);
			if (pos < _text.Length)
				return _text[pos];
			return NullChar;
		}

		/// <summary>
		/// Extracts a substring from the specified position to the end of the text
		/// </summary>
		/// <param name="start"></param>
		/// <returns></returns>
		public string Substring(int start) {
			return Substring(start, _text.Length);
		}

		/// <summary>
		/// Extracts a substring from the specified range of the current text
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public string Substring(int start, int end) {
			return _text.Substring(start, end - start);
		}

		/// <summary>
		/// Moves the current position ahead one character
		/// </summary>
		public void MoveAhead() {
			MoveAhead(1);
		}

		/// <summary>
		/// Moves the current position ahead the specified number of characters
		/// </summary>
		/// <param name="ahead">The number of characters to move ahead</param>
		public void MoveAhead(int ahead) {
			_pos = Math.Min(_pos + ahead, _text.Length);
		}

		/// <summary>
		/// Moves to the next occurrence of the specified string
		/// </summary>
		/// <param name="s">String to find</param>
		/// <param name="ignoreCase">Indicates if case-insensitive comparisons are used</param>
		public void MoveTo(string s, bool ignoreCase = false) {
			_pos = _text.IndexOf(s, _pos,
				ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			if (_pos < 0)
				_pos = _text.Length;
		}

		/// <summary>
		/// Moves to the next occurrence of the specified character
		/// </summary>
		/// <param name="c">Character to find</param>
		public void MoveTo(char c) {
			_pos = _text.IndexOf(c, _pos);
			if (_pos < 0)
				_pos = _text.Length;
		}

		/// <summary>
		/// Moves to the next occurrence of any one of the specified
		/// characters
		/// </summary>
		/// <param name="carr">Array of characters to find</param>
		public void MoveTo(char[] carr) {
			_pos = _text.IndexOfAny(carr, _pos);
			if (_pos < 0)
				_pos = _text.Length;
		}

		/// <summary>
		/// Moves the current position to the first character that is part of a newline
		/// </summary>
		public void MoveToEndOfLine() {
			char c = Peek();
			while (c != '\r' && c != '\n' && !EndOfText) {
				MoveAhead();
				c = Peek();
			}
		}

		/// <summary>
		/// Moves the current position to the next character that is not whitespace
		/// </summary>
		public void MovePastWhitespace() {
			while (Char.IsWhiteSpace(Peek()))
				MoveAhead();
		}
	}
}