using System;
using System.Collections.Generic;
using System.Text;

public class Generator
{
    private class Var
    {
        public string Name;
        public int StackLoc;
    }

    private NodeProg m_prog;
    private StringBuilder m_output = new StringBuilder();
    private int m_stackSize = 0;
    private List<Var> m_vars = new List<Var>();
    private List<int> m_scopes = new List<int>();
    private int m_labelCount = 0;

    public Generator(NodeProg prog)
    {
        m_prog = prog;
    }

    public void GenTerm(NodeTerm term)
    {
        if (term.Var is NodeTermIntLit intLit)
        {
            m_output.Append("    mov rax, " + intLit.IntLit.Value + "\n");
            Push("rax");
        }
        else if (term.Var is NodeTermIdent termIdent)
        {
            var found = m_vars.Find(v => v.Name == termIdent.Ident.Value);
            if (found == null)
            {
                Console.Error.WriteLine("Undeclared identifier: " + termIdent.Ident.Value);
                Environment.Exit(1);
            }
            string offset = "QWORD [rsp + " + ((m_stackSize - found.StackLoc - 1) * 8) + "]";
            Push(offset);
        }
        else if (term.Var is NodeTermParen paren)
        {
            GenExpr(paren.Expr);
        }
    }

    public void GenBinExpr(NodeBinExpr binExpr)
    {
        if (binExpr.Var is NodeBinExprSub sub)
        {
            GenExpr(sub.Rhs);
            GenExpr(sub.Lhs);
            Pop("rax");
            Pop("rbx");
            m_output.Append("    sub rax, rbx\n");
            Push("rax");
        }
        else if (binExpr.Var is NodeBinExprAdd add)
        {
            GenExpr(add.Rhs);
            GenExpr(add.Lhs);
            Pop("rax");
            Pop("rbx");
            m_output.Append("    add rax, rbx\n");
            Push("rax");
        }
        else if (binExpr.Var is NodeBinExprMulti multi)
        {
            GenExpr(multi.Rhs);
            GenExpr(multi.Lhs);
            Pop("rax");
            Pop("rbx");
            m_output.Append("    mul rbx\n");
            Push("rax");
        }
        else if (binExpr.Var is NodeBinExprDiv div)
        {
            GenExpr(div.Rhs);
            GenExpr(div.Lhs);
            Pop("rax");
            Pop("rbx");
            m_output.Append("    div rbx\n");
            Push("rax");
        }
    }

    public void GenExpr(NodeExpr expr)
    {
        if (expr.Var is NodeTerm term)
            GenTerm(term);
        else if (expr.Var is NodeBinExpr binExpr)
            GenBinExpr(binExpr);
    }

    public void GenScope(NodeScope scope)
    {
        BeginScope();
        foreach (var stmt in scope.Stmts)
            GenStmt(stmt);
        EndScope();
    }

    public void GenIfPred(NodeIfPred pred, string endLabel)
    {
        if (pred.Var is NodeIfPredElif elif)
        {
            m_output.Append("    ;; elif\n");
            GenExpr(elif.Expr);
            Pop("rax");
            string label = CreateLabel();
            m_output.Append("    test rax, rax\n");
            m_output.Append("    jz " + label + "\n");
            GenScope(elif.Scope);
            m_output.Append("    jmp " + endLabel + "\n");
            if (elif.Pred != null)
            {
                m_output.Append(label + ":\n");
                GenIfPred(elif.Pred, endLabel);
            }
        }
        else if (pred.Var is NodeIfPredElse else_)
        {
            m_output.Append("    ;; else\n");
            GenScope(else_.Scope);
        }
    }

    public void GenStmt(NodeStmt stmt)
    {
        if (stmt.Var is NodeStmtExit stmtExit)
        {
            m_output.Append("    ;; exit\n");
            GenExpr(stmtExit.Expr);
            m_output.Append("    mov rax, 60\n");
            Pop("rdi");
            m_output.Append("    syscall\n");
            m_output.Append("    ;; /exit\n");
        }
        else if (stmt.Var is NodeStmtLet stmtLet)
        {
            m_output.Append("    ;; let\n");
            if (m_vars.Find(v => v.Name == stmtLet.Ident.Value) != null)
            {
                Console.Error.WriteLine("Identifier already used: " + stmtLet.Ident.Value);
                Environment.Exit(1);
            }
            m_vars.Add(new Var { Name = stmtLet.Ident.Value, StackLoc = m_stackSize });
            GenExpr(stmtLet.Expr);
            m_output.Append("    ;; /let\n");
        }
        else if (stmt.Var is NodeStmtAssign stmtAssign)
        {
            var found = m_vars.Find(v => v.Name == stmtAssign.Ident.Value);
            if (found == null)
            {
                Console.Error.WriteLine("Undeclared identifier: " + stmtAssign.Ident.Value);
                Environment.Exit(1);
            }
            GenExpr(stmtAssign.Expr);
            Pop("rax");
            m_output.Append("    mov [rsp + " + ((m_stackSize - found.StackLoc - 1) * 8) + "], rax\n");
        }
        else if (stmt.Var is NodeScope scope)
        {
            m_output.Append("    ;; scope\n");
            GenScope(scope);
            m_output.Append("    ;; /scope\n");
        }
        else if (stmt.Var is NodeStmtIf stmtIf)
        {
            m_output.Append("    ;; if\n");
            GenExpr(stmtIf.Expr);
            Pop("rax");
            string label = CreateLabel();
            m_output.Append("    test rax, rax\n");
            m_output.Append("    jz " + label + "\n");
            GenScope(stmtIf.Scope);
            if (stmtIf.Pred != null)
            {
                string endLabel = CreateLabel();
                m_output.Append("    jmp " + endLabel + "\n");
                m_output.Append(label + ":\n");
                GenIfPred(stmtIf.Pred, endLabel);
                m_output.Append(endLabel + ":\n");
            }
            else
            {
                m_output.Append(label + ":\n");
            }
            m_output.Append("    ;; /if\n");
        }
    }

    public string GenProg()
    {
        m_output.Append("global _start\n_start:\n");

        foreach (var stmt in m_prog.Stmts)
            GenStmt(stmt);

        m_output.Append("    mov rax, 60\n");
        m_output.Append("    mov rdi, 0\n");
        m_output.Append("    syscall\n");

        return m_output.ToString();
    }

    private void Push(string reg)
    {
        m_output.Append("    push " + reg + "\n");
        m_stackSize++;
    }

    private void Pop(string reg)
    {
        m_output.Append("    pop " + reg + "\n");
        m_stackSize--;
    }

    private void BeginScope()
    {
        m_scopes.Add(m_vars.Count);
    }

    private void EndScope()
    {
        int popCount = m_vars.Count - m_scopes[m_scopes.Count - 1];
        if (popCount != 0)
            m_output.Append("    add rsp, " + popCount * 8 + "\n");

        m_stackSize -= popCount;
        for (int i = 0; i < popCount; i++)
            m_vars.RemoveAt(m_vars.Count - 1);

        m_scopes.RemoveAt(m_scopes.Count - 1);
    }

    private string CreateLabel()
    {
        return "label" + m_labelCount++;
    }
}