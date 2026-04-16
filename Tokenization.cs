using System;
using System.Collections.Generic;

public enum TokenType
{
    Exit,
    IntLit,
    Semi,
    OpenParen,
    CloseParen,
    Ident,
    Let,
    Eq,
    Plus,
    Star,
    Minus,
    FSlash,
    OpenCurly,
    CloseCurly,
    If,
    Elif,
    Else,
}

public static class TokenTypeHelper
{
    public static string ToString(TokenType type)
    {
        switch (type)
        {
            case TokenType.Exit:       return "`exit`";
            case TokenType.IntLit:     return "int literal";
            case TokenType.Semi:       return "`;`";
            case TokenType.OpenParen:  return "`(`";
            case TokenType.CloseParen: return "`)`";
            case TokenType.Ident:      return "identifier";
            case TokenType.Let:        return "`let`";
            case TokenType.Eq:         return "`=`";
            case TokenType.Plus:       return "`+`";
            case TokenType.Star:       return "`*`";
            case TokenType.Minus:      return "`-`";
            case TokenType.FSlash:     return "`/`";
            case TokenType.OpenCurly:  return "`{`";
            case TokenType.CloseCurly: return "`}`";
            case TokenType.If:         return "`if`";
            case TokenType.Elif:       return "`elif`";
            case TokenType.Else:       return "`else`";
            default: throw new Exception("Unreachable");
        }
    }

    public static int? BinPrec(TokenType type)
    {
        switch (type)
        {
            case TokenType.Minus:
            case TokenType.Plus:
                return 0;
            case TokenType.FSlash:
            case TokenType.Star:
                return 1;
            default:
                return null;
        }
    }
}

public class Token
{
    public TokenType Type;
    public int Line;
    public string Value;

    public Token(TokenType type, int line, string value = null)
    {
        Type = type;
        Line = line;
        Value = value;
    }
}

public class Tokenizer
{
    private string m_src;
    private int m_index = 0;

    public Tokenizer(string src)
    {
        m_src = src;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        var buf = "";
        int lineCount = 1;

        while (Peek() != null)
        {
            if (char.IsLetter(Peek().Value))
            {
                buf += Consume();
                while (Peek() != null && char.IsLetterOrDigit(Peek().Value))
                    buf += Consume();

                if (buf == "exit")
                    tokens.Add(new Token(TokenType.Exit, lineCount));
                else if (buf == "let")
                    tokens.Add(new Token(TokenType.Let, lineCount));
                else if (buf == "if")
                    tokens.Add(new Token(TokenType.If, lineCount));
                else if (buf == "elif")
                    tokens.Add(new Token(TokenType.Elif, lineCount));
                else if (buf == "else")
                    tokens.Add(new Token(TokenType.Else, lineCount));
                else
                    tokens.Add(new Token(TokenType.Ident, lineCount, buf));

                buf = "";
            }
            else if (char.IsDigit(Peek().Value))
            {
                buf += Consume();
                while (Peek() != null && char.IsDigit(Peek().Value))
                    buf += Consume();

                tokens.Add(new Token(TokenType.IntLit, lineCount, buf));
                buf = "";
            }
            else if (Peek().Value == '/' && Peek(1) != null && Peek(1).Value == '/')
            {
                Consume();
                Consume();
                while (Peek() != null && Peek().Value != '\n')
                    Consume();
            }
            else if (Peek().Value == '/' && Peek(1) != null && Peek(1).Value == '*')
            {
                Consume();
                Consume();
                while (Peek() != null)
                {
                    if (Peek().Value == '*' && Peek(1) != null && Peek(1).Value == '/')
                        break;
                    Consume();
                }
                if (Peek() != null) Consume();
                if (Peek() != null) Consume();
            }
            else if (Peek().Value == '(')  { Consume(); tokens.Add(new Token(TokenType.OpenParen,  lineCount)); }
            else if (Peek().Value == ')')  { Consume(); tokens.Add(new Token(TokenType.CloseParen, lineCount)); }
            else if (Peek().Value == ';')  { Consume(); tokens.Add(new Token(TokenType.Semi,        lineCount)); }
            else if (Peek().Value == '=')  { Consume(); tokens.Add(new Token(TokenType.Eq,          lineCount)); }
            else if (Peek().Value == '+')  { Consume(); tokens.Add(new Token(TokenType.Plus,        lineCount)); }
            else if (Peek().Value == '*')  { Consume(); tokens.Add(new Token(TokenType.Star,        lineCount)); }
            else if (Peek().Value == '-')  { Consume(); tokens.Add(new Token(TokenType.Minus,       lineCount)); }
            else if (Peek().Value == '/')  { Consume(); tokens.Add(new Token(TokenType.FSlash,      lineCount)); }
            else if (Peek().Value == '{')  { Consume(); tokens.Add(new Token(TokenType.OpenCurly,   lineCount)); }
            else if (Peek().Value == '}')  { Consume(); tokens.Add(new Token(TokenType.CloseCurly,  lineCount)); }
            else if (Peek().Value == '\n') { Consume(); lineCount++; }
            else if (char.IsWhiteSpace(Peek().Value)) { Consume(); }
            else
            {
                Console.Error.WriteLine("Invalid token");
                System.Environment.Exit(1);
            }
        }

        m_index = 0;
        return tokens;
    }

    private char? Peek(int offset = 0)
    {
        if (m_index + offset >= m_src.Length)
            return null;
        return m_src[m_index + offset];
    }

    private char Consume()
    {
        return m_src[m_index++];
    }
}