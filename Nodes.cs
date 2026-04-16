using System.Collections.Generic;

public class NodeTermIntLit  { public Token IntLit; }
public class NodeTermIdent   { public Token Ident; }
public class NodeTermParen   { public NodeExpr Expr; }

public class NodeTerm
{
    public object Var; // NodeTermIntLit | NodeTermIdent | NodeTermParen
}

public class NodeBinExprAdd   { public NodeExpr Lhs; public NodeExpr Rhs; }
public class NodeBinExprMulti { public NodeExpr Lhs; public NodeExpr Rhs; }
public class NodeBinExprSub   { public NodeExpr Lhs; public NodeExpr Rhs; }
public class NodeBinExprDiv   { public NodeExpr Lhs; public NodeExpr Rhs; }

public class NodeBinExpr
{
    public object Var; // NodeBinExprAdd | NodeBinExprMulti | NodeBinExprSub | NodeBinExprDiv
}

public class NodeExpr
{
    public object Var; // NodeTerm | NodeBinExpr
}

public class NodeStmtExit   { public NodeExpr Expr; }
public class NodeStmtLet    { public Token Ident; public NodeExpr Expr; }
public class NodeStmtAssign { public Token Ident; public NodeExpr Expr; }

public class NodeScope
{
    public List<NodeStmt> Stmts = new List<NodeStmt>();
}

public class NodeIfPredElif
{
    public NodeExpr Expr;
    public NodeScope Scope;
    public NodeIfPred Pred; // nullable
}

public class NodeIfPredElse
{
    public NodeScope Scope;
}

public class NodeIfPred
{
    public object Var; // NodeIfPredElif | NodeIfPredElse
}

public class NodeStmtIf
{
    public NodeExpr Expr;
    public NodeScope Scope;
    public NodeIfPred Pred; // nullable
}

public class NodeStmt
{
    public object Var; // NodeStmtExit | NodeStmtLet | NodeScope | NodeStmtIf | NodeStmtAssign
}

public class NodeProg
{
    public List<NodeStmt> Stmts = new List<NodeStmt>();
}