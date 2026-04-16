using System;
using System.Collections.Generic;

public class Parser
{
    private List<Token> m_tokens;
    private int m_index = 0;

    public Parser(List<Token> tokens)
    {
        m_tokens = tokens;
    }

    public void ErrorExpected(string msg)
    {
        Console.Error.WriteLine("[ERR] Expected " + msg + " on line " + Peek(-1).Line);
        Environment.Exit(1);
    }

    public NodeTerm ParseTerm()
    {
        if (TryConsume(TokenType.IntLit) is Token intLit)
        {
            return new NodeTerm { Var = new NodeTermIntLit { IntLit = intLit } };
        }
        if (TryConsume(TokenType.Ident) is Token ident)
        {
            return new NodeTerm { Var = new NodeTermIdent { Ident = ident } };
        }
        if (TryConsume(TokenType.OpenParen) is Token _)
        {
            var expr = ParseExpr();
            if (expr == null) ErrorExpected("expression");
            TryConsumeErr(TokenType.CloseParen);
            return new NodeTerm { Var = new NodeTermParen { Expr = expr } };// expr op
        }
        return null;
    }

    public NodeExpr ParseExpr(int minPrec = 0)
    {
        var termLhs = ParseTerm();
        if (termLhs == null) return null;

        var exprLhs = new NodeExpr { Var = termLhs };

        while (true)
        {
            var currTok = Peek();
            if (currTok == null) break;

            var prec = TokenTypeHelper.BinPrec(currTok.Type);
            if (prec == null || prec < minPrec) break;

            var opToken = Consume();
            int nextMinPrec = prec.Value + 1;

            var exprRhs = ParseExpr(nextMinPrec);
            if (exprRhs == null) ErrorExpected("expression");

            var binExpr = new NodeBinExpr();
            var exprLhs2 = new NodeExpr { Var = exprLhs.Var };

            if (opToken.Type == TokenType.Plus)
                binExpr.Var = new NodeBinExprAdd   { Lhs = exprLhs2, Rhs = exprRhs };
            else if (opToken.Type == TokenType.Star)
                binExpr.Var = new NodeBinExprMulti { Lhs = exprLhs2, Rhs = exprRhs };
            else if (opToken.Type == TokenType.Minus)
                binExpr.Var = new NodeBinExprSub   { Lhs = exprLhs2, Rhs = exprRhs };
            else if (opToken.Type == TokenType.FSlash)
                binExpr.Var = new NodeBinExprDiv   { Lhs = exprLhs2, Rhs = exprRhs };
            else
                throw new Exception("Unreachable");

            exprLhs.Var = binExpr;
        }

        return exprLhs;
    }

    public NodeScope ParseScope()
    {
        if (TryConsume(TokenType.OpenCurly) == null) return null;

        var scope = new NodeScope();
        NodeStmt stmt;
        while ((stmt = ParseStmt()) != null)
            scope.Stmts.Add(stmt);

        TryConsumeErr(TokenType.CloseCurly);
        return scope;
    }

    public NodeIfPred ParseIfPred()
    {
        if (TryConsume(TokenType.Elif) != null)
        {
            TryConsumeErr(TokenType.OpenParen);
            var elif = new NodeIfPredElif();

            var expr = ParseExpr();
            if (expr != null) elif.Expr = expr;
            else ErrorExpected("expression");

            TryConsumeErr(TokenType.CloseParen);

            var scope = ParseScope();
            if (scope != null) elif.Scope = scope;
            else ErrorExpected("scope");

            elif.Pred = ParseIfPred();

            return new NodeIfPred { Var = elif };
        }

        if (TryConsume(TokenType.Else) != null)
        {
            var else_ = new NodeIfPredElse();

            var scope = ParseScope();
            if (scope != null) else_.Scope = scope;
            else ErrorExpected("scope");

            return new NodeIfPred { Var = else_ };
        }

        return null;
    }

    public NodeStmt ParseStmt()
    {
        if (Peek() != null && Peek().Type == TokenType.Exit &&
            Peek(1) != null && Peek(1).Type == TokenType.OpenParen)
        {
            Consume();
            Consume();
            var stmtExit = new NodeStmtExit();

            var expr = ParseExpr();
            if (expr != null) stmtExit.Expr = expr;
            else ErrorExpected("expression");

            TryConsumeErr(TokenType.CloseParen);
            TryConsumeErr(TokenType.Semi);

            return new NodeStmt { Var = stmtExit };
        }

        if (Peek() != null && Peek().Type == TokenType.Let &&
            Peek(1) != null && Peek(1).Type == TokenType.Ident &&
            Peek(2) != null && Peek(2).Type == TokenType.Eq)
        {
            Consume();
            var stmtLet = new NodeStmtLet();
            stmtLet.Ident = Consume();
            Consume();

            var expr = ParseExpr();
            if (expr != null) stmtLet.Expr = expr;
            else ErrorExpected("expression");

            TryConsumeErr(TokenType.Semi);
            return new NodeStmt { Var = stmtLet };
        }

        if (Peek() != null && Peek().Type == TokenType.Ident &&
            Peek(1) != null && Peek(1).Type == TokenType.Eq)
        {
            var assign = new NodeStmtAssign();
            assign.Ident = Consume();
            Consume();

            var expr = ParseExpr();
            if (expr != null) assign.Expr = expr;
            else ErrorExpected("expression");

            TryConsumeErr(TokenType.Semi);
            return new NodeStmt { Var = assign };
        }

        if (Peek() != null && Peek().Type == TokenType.OpenCurly)
        {
            var scope = ParseScope();
            if (scope != null) return new NodeStmt { Var = scope };
            ErrorExpected("scope");
        }

        if (TryConsume(TokenType.If) != null)
        {
            TryConsumeErr(TokenType.OpenParen);
            var stmtIf = new NodeStmtIf();

            var expr = ParseExpr();
            if (expr != null) stmtIf.Expr = expr;
            else ErrorExpected("expression");

            TryConsumeErr(TokenType.CloseParen);

            var scope = ParseScope();
            if (scope != null) stmtIf.Scope = scope;
            else ErrorExpected("scope");

            stmtIf.Pred = ParseIfPred();
            return new NodeStmt { Var = stmtIf };
        }

        return null;
    }
    // if toke
    public NodeProg ParseProg() {
        var prog = new NodeProg();
        while (Peek() != null)
        {
            var stmt = ParseStmt();
            if (stmt != null)
                prog.Stmts.Add(stmt);
            else
                ErrorExpected("statement");
        }
        return prog;   }
    private Token Peek(int offset = 0)
    {
        if (m_index + offset >= m_tokens.Count || m_index + offset < 0)
            return null;
        return m_tokens[m_index + offset];
    }

    private Token Consume()
    {
        return m_tokens[m_index++];
    }

    private Token TryConsumeErr(TokenType type)
    {
        if (Peek() != null && Peek().Type == type)
            return Consume();
        ErrorExpected(TokenTypeHelper.ToString(type));
        return null;
    }

    private Token TryConsume(TokenType type)
    {
        if (Peek() != null && Peek().Type == type)
            return Consume();
        return null;
    }
}