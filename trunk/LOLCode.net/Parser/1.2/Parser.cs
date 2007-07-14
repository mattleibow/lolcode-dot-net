using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

using System;
using System.CodeDom.Compiler;

namespace notdot.LOLCode.Parser.v1_2 {



internal partial class Parser {
	const int _EOF = 0;
	const int _ident = 1;
	const int _intCon = 2;
	const int _realCon = 3;
	const int _stringCon = 4;
	const int _eos = 5;
	const int _can = 6;
	const int _in = 7;
	const int _im = 8;
	const int _outta = 9;
	const int _mkay = 10;
	const int _r = 11;
	const int _is = 12;
	const int _how = 13;
	const int maxT = 63;
	const int _comment = 64;
	const int _blockcomment = 65;
	const int _continuation = 66;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		//errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(filename, la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(filename, t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }
				if (la.kind == 64) {
				}
				if (la.kind == 65) {
				}
				if (la.kind == 66) {
				}

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}
	
	bool WeakSeparator (int n, int syFol, int repFol) {
		bool[] s = new bool[maxT+1];
		if (la.kind == n) { Get(); return true; }
		else if (StartOf(repFol)) return false;
		else {
			for (int i=0; i <= maxT; i++) {
				s[i] = set[syFol, i] || set[repFol, i] || set[0, i];
			}
			SynErr(n);
			while (!s[la.kind]) Get();
			return StartOf(syFol);
		}
	}
	
	void LOLCode() {
		Expect(14);
		if (la.kind == 15) {
			Get();
			Expect(16);
			program.version = LOLCodeVersion.v1_2; 
		}
		while (la.kind == 5) {
			Get();
		}
		Statements(out program.methods["Main"].statements);
		Expect(17);
		while (la.kind == 5) {
			Get();
		}
	}

	void Statements(out Statement stat) {
		BlockStatement bs = new BlockStatement(GetPragma(t)); stat = bs; 
		Statement s; 
		while ((StartOf(1) || la.kind == _can || la.kind == _how) && scanner.Peek().kind != _outta) {
			if (la.kind == 6) {
				CanHasStatement();
			} else if (la.kind == 13) {
				FunctionDeclaration();
			} else if (StartOf(1)) {
				Statement(out s);
				bs.statements.Add(s); 
			} else SynErr(64);
			while (la.kind == 5) {
				Get();
			}
		}
	}

	void CanHasStatement() {
		StringBuilder sb = new StringBuilder(); 
		Expect(6);
		Expect(19);
		Expect(1);
		sb.Append(t.val); 
		while (la.kind == 22) {
			Get();
			Expect(1);
			sb.Append('.'); sb.Append(t.val); 
		}
		Expect(23);
		if(!program.ImportLibrary(sb.ToString())) Error(string.Format("Library \"{0}\" not found.", sb.ToString())); 
	}

	void FunctionDeclaration() {
		if(currentMethod != null) Error("Cannot define a function inside another function."); 
		Expect(13);
		Expect(57);
		Expect(18);
		Expect(1);
		currentMethod = new LOLMethod(GetFunction(t.val), program); short arg = 0; 
		if (la.kind == 30) {
			Get();
			Expect(1);
			currentMethod.SetArgumentName(arg++, t.val); 
			while (la.kind == 48) {
				Get();
				Expect(30);
				Expect(1);
				currentMethod.SetArgumentName(arg++, t.val); 
			}
		}
		while (la.kind == 5) {
			Get();
		}
		Statements(out currentMethod.statements);
		Expect(58);
		Expect(59);
		Expect(60);
		Expect(61);
		program.methods.Add(currentMethod.info.Name, currentMethod); currentMethod = null; 
	}

	void Statement(out Statement stat) {
		stat = null; 
		if (la.kind == 18) {
			IHasAStatement(out stat);
		} else if (TokenAfterLValue().kind == _r) {
			AssignmentStatement(out stat);
		} else if (TokenAfterLValue().kind == _is) {
			TypecastStatement(out stat);
		} else if (la.kind == 24) {
			GimmehStatement(out stat);
		} else if (la.kind == 8) {
			LoopStatement(out stat);
		} else if (la.kind == 28) {
			BreakStatement(out stat);
		} else if (la.kind == 29) {
			ContinueStatement(out stat);
		} else if (la.kind == 33) {
			OrlyStatement(out stat);
		} else if (la.kind == 40) {
			SwitchStatement(out stat);
		} else if (la.kind == 43 || la.kind == 44) {
			PrintStatement(out stat);
		} else if (StartOf(2)) {
			ExpressionStatement(out stat);
		} else if (la.kind == 62) {
			ReturnStatement(out stat);
		} else SynErr(65);
	}

	void IHasAStatement(out Statement stat) {
		VariableDeclarationStatement vds = new VariableDeclarationStatement(GetPragma(la)); stat = vds; 
		Expect(18);
		Expect(19);
		Expect(20);
		Expect(1);
		vds.var = DeclareVariable(t.val); SetEndPragma(stat); 
		if (la.kind == 21) {
			Get();
			Expression(out vds.expression);
		}
	}

	void AssignmentStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); stat = ass; 
		LValue(out ass.lval);
		Expect(11);
		Expression(out ass.rval);
		SetEndPragma(stat); 
	}

	void TypecastStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); stat = ass; Type t; 
		LValue(out ass.lval);
		Expect(12);
		Expect(46);
		Expect(20);
		Typename(out t);
		SetEndPragma(ass); ass.rval = new TypecastExpression(ass.location, t, ass.lval); 
	}

	void GimmehStatement(out Statement stat) {
		InputStatement ins = new InputStatement(GetPragma(la)); stat = ins; 
		Expect(24);
		if (la.kind == 25 || la.kind == 26 || la.kind == 27) {
			if (la.kind == 25) {
				Get();
			} else if (la.kind == 26) {
				Get();
				ins.amount = IOAmount.Word; 
			} else {
				Get();
				ins.amount = IOAmount.Letter; 
			}
		}
		LValue(out ins.dest);
		SetEndPragma(stat); 
	}

	void LoopStatement(out Statement stat) {
		LoopStatement ls = new LoopStatement(GetPragma(la)); stat = ls; 
		Expect(8);
		Expect(7);
		Expect(30);
		Expect(1);
		ls.name = t.val; BeginScope(); 
		if (la.kind == 1) {
			ls.StartOperation(GetPragma(la)); 
			Get();
			ls.SetOperationFunction(GetFunction(t.val)); 
			if (la.kind == 30) {
				Get();
			}
			Expect(1);
			ls.SetLoopVariable(GetPragma(t), GetVariable(t.val)); SetEndPragma(ls.operation); SetEndPragma(((ls.operation as AssignmentStatement).rval as FunctionExpression).arguments[0]); SetEndPragma((ls.operation as AssignmentStatement).rval); SetEndPragma((ls.operation as AssignmentStatement).lval); 
		}
		if (la.kind == 31 || la.kind == 32) {
			if (la.kind == 31) {
				Get();
				ls.type = LoopType.Until; 
			} else {
				Get();
				ls.type = LoopType.While; 
			}
			Expression(out ls.condition);
		}
		SetEndPragma(stat); 
		Expect(5);
		while (la.kind == 5) {
			Get();
		}
		Statements(out ls.statements);
		Expect(8);
		Expect(9);
		Expect(30);
		Expect(1);
		if(t.val != ls.name) Error("Loop terminator label does not match loop label"); EndScope(); 
	}

	void BreakStatement(out Statement stat) {
		BreakStatement bs = new BreakStatement(GetPragma(la)); stat = bs; 
		Expect(28);
		if (la.kind == 1) {
			Get();
			bs.label = t.val; 
		}
		Expect(5);
		SetEndPragma(stat); 
	}

	void ContinueStatement(out Statement stat) {
		ContinueStatement cs = new ContinueStatement(GetPragma(la)); stat = cs; 
		Expect(29);
		if (la.kind == 1) {
			Get();
			cs.label = t.val; 
		}
		Expect(5);
		SetEndPragma(stat); 
	}

	void OrlyStatement(out Statement stat) {
		ConditionalStatement cs = new ConditionalStatement(GetPragma(la)); stat = cs; ConditionalStatement cur = cs; Statement st; Expression e; cs.condition = new VariableLValue(GetPragma(la), GetVariable("IT")); 
		Expect(33);
		Expect(34);
		Expect(23);
		SetEndPragma(cs); 
		while (la.kind == 5) {
			Get();
		}
		if (la.kind == 35) {
			Get();
			Expect(34);
			while (la.kind == 5) {
				Get();
			}
		}
		BeginScope(); 
		Statements(out cs.trueStatements);
		EndScope(); 
		while (la.kind == 36) {
			Get();
			cur.falseStatements = new ConditionalStatement(GetPragma(la)); 
			Expression(out e);
			(cur.falseStatements as ConditionalStatement).condition = e; SetEndPragma(cur.falseStatements); cur = (ConditionalStatement)cur.falseStatements; BeginScope(); 
			while (la.kind == 5) {
				Get();
			}
			Statements(out cur.trueStatements);
			EndScope(); 
		}
		if (la.kind == 37) {
			Get();
			Expect(38);
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.falseStatements);
			EndScope(); 
		}
		Expect(39);
	}

	void SwitchStatement(out Statement stat) {
		SwitchStatement ss = new SwitchStatement(GetPragma(la)); ss.control = new VariableLValue(GetPragma(la), GetVariable("IT")); stat = ss; Object label; Statement block; 
		Expect(40);
		Expect(23);
		SetEndPragma(ss); 
		while (la.kind == 5) {
			Get();
		}
		while (la.kind == 41) {
			Get();
			SwitchLabel(out label);
			while (la.kind == 5) {
				Get();
			}
			Statements(out block);
			AddCase(ss, label, block); 
		}
		if (la.kind == 42) {
			Get();
			while (la.kind == 5) {
				Get();
			}
			Statements(out ss.defaultCase);
		}
		Expect(39);
	}

	void PrintStatement(out Statement stat) {
		PrintStatement ps = new PrintStatement(GetPragma(la)); ps.message = new FunctionExpression(GetPragma(la), GetFunction("SMOOSH")); stat = ps; Expression e;
		if (la.kind == 43) {
			Get();
		} else if (la.kind == 44) {
			Get();
			ps.stderr = true; 
		} else SynErr(66);
		while (StartOf(2)) {
			Expression(out e);
			(ps.message as FunctionExpression).arguments.Add(e); 
		}
		if (la.kind == 45) {
			Get();
			ps.newline = false; 
		}
		Expect(5);
		SetEndPragma(stat); 
	}

	void ExpressionStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); ass.lval = new VariableLValue(GetPragma(la), GetVariable("IT")); stat = ass; Expression exp;
		Expression(out exp);
		ass.rval = exp; SetEndPragma(ass); 
	}

	void ReturnStatement(out Statement stat) {
		ReturnStatement rs = new ReturnStatement(GetPragma(la)); stat = rs; 
		Expect(62);
		Expect(30);
		Expression(out rs.expression);
	}

	void Expression(out Expression exp) {
		exp = null; 
		if (IsFunction(la.val)) {
			FunctionExpression(out exp);
		} else if (la.kind == 49) {
			TypecastExpression(out exp);
		} else if (StartOf(3)) {
			Unary(out exp);
		} else SynErr(67);
	}

	void LValue(out LValue lv) {
		lv = null; 
		Expect(1);
		lv = new VariableLValue(GetPragma(t), GetVariable(t.val)); SetEndPragma(lv); 
	}

	void SwitchLabel(out object obj) {
		obj = null; 
		if (StartOf(4)) {
			Const(out obj);
		} else if (la.kind == 4) {
			Get();
			List<VariableRef> refs = new List<VariableRef>(); obj = notdot.LOLCode.StringExpression.UnescapeString(t.val, GetScope(), errors, GetPragma(t), refs); if(refs.Count > 0) Error("String constants in OMG labels cannot contain variable substitutions."); 
		} else SynErr(68);
	}

	void Const(out object val) {
		val = null; 
		if (la.kind == 2) {
			Get();
			val = int.Parse(t.val); 
		} else if (la.kind == 3) {
			Get();
			val = float.Parse(t.val); 
		} else if (la.kind == 54) {
			Get();
			val = null; 
		} else if (la.kind == 55) {
			Get();
			val = true; 
		} else if (la.kind == 56) {
			Get();
			val = false; 
		} else SynErr(69);
	}

	void Typename(out Type t) {
		t = typeof(void); 
		if (la.kind == 50) {
			Get();
			t = typeof(bool); 
		} else if (la.kind == 51) {
			Get();
			t = typeof(int); 
		} else if (la.kind == 52) {
			Get();
			t = typeof(float); 
		} else if (la.kind == 53) {
			Get();
			t = typeof(string); 
		} else SynErr(70);
	}

	void FunctionExpression(out Expression exp) {
		FunctionExpression fe = new FunctionExpression(GetPragma(la)); exp = fe; Expression e2; 
		Expect(1);
		fe.func = GetFunction(t.val); int argsLeft = fe.func.Arity; 
		if (la.kind == 47) {
			Get();
		}
		if ((argsLeft > 0 || (fe.func.IsVariadic && la.kind != _mkay)) && la.kind != _eos) {
			Expression(out e2);
			fe.arguments.Add(e2); argsLeft--; 
			while ((argsLeft > 0 || (fe.func.IsVariadic && la.kind != _mkay)) && la.kind != _eos) {
				if (la.kind == 48) {
					Get();
				}
				Expression(out e2);
				fe.arguments.Add(e2); argsLeft--; 
			}
		}
		if (fe.func.IsVariadic && la.kind != _eos) {
			Expect(10);
		}
	}

	void TypecastExpression(out Expression exp) {
		TypecastExpression te = new TypecastExpression(GetPragma(la)); exp = te; 
		Expect(49);
		Expression(out te.exp);
		if (la.kind == 20) {
			Get();
		}
		Typename(out te.destType);
		SetEndPragma(te); 
	}

	void Unary(out Expression exp) {
		exp = null; Object val; LValue lv; 
		if (StartOf(4)) {
			Const(out val);
			exp = new PrimitiveExpression(GetPragma(t), val); SetEndPragma(exp); 
		} else if (la.kind == 4) {
			StringExpression(out exp);
		} else if (la.kind == 1) {
			LValue(out lv);
			exp = lv; SetEndPragma(exp); 
		} else SynErr(71);
	}

	void StringExpression(out Expression exp) {
		Expect(4);
		exp = new StringExpression(GetPragma(t), t.val, GetScope(), errors); SetEndPragma(exp); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		LOLCode();

    Expect(0);
	}
	
	bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,x, T,T,x,x, x,T,x,x, x,x,x,x, T,x,x,T, T,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,x,T,x, x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x,x, x,x,x,x, x},
		{x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x,x, x,x,x,x, x}

	};
} // end Parser


public class Errors {
	//public string errMsgFormat = "-- \"{0}\" line {1} col {2}: {3}"; // 0=file, 1=line, 2=column, 3=text

	private CompilerErrorCollection cec;
	
	public Errors(CompilerErrorCollection c) {
		this.cec = c;
	}
  
	public void SynErr (string file, int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "intCon expected"; break;
			case 3: s = "realCon expected"; break;
			case 4: s = "stringCon expected"; break;
			case 5: s = "eos expected"; break;
			case 6: s = "can expected"; break;
			case 7: s = "in expected"; break;
			case 8: s = "im expected"; break;
			case 9: s = "outta expected"; break;
			case 10: s = "mkay expected"; break;
			case 11: s = "r expected"; break;
			case 12: s = "is expected"; break;
			case 13: s = "how expected"; break;
			case 14: s = "\"HAI\" expected"; break;
			case 15: s = "\"TO\" expected"; break;
			case 16: s = "\"1.2\" expected"; break;
			case 17: s = "\"KTHXBYE\" expected"; break;
			case 18: s = "\"I\" expected"; break;
			case 19: s = "\"HAS\" expected"; break;
			case 20: s = "\"A\" expected"; break;
			case 21: s = "\"ITZ\" expected"; break;
			case 22: s = "\".\" expected"; break;
			case 23: s = "\"?\" expected"; break;
			case 24: s = "\"GIMMEH\" expected"; break;
			case 25: s = "\"LINE\" expected"; break;
			case 26: s = "\"WORD\" expected"; break;
			case 27: s = "\"LETTAR\" expected"; break;
			case 28: s = "\"GTFO\" expected"; break;
			case 29: s = "\"MOAR\" expected"; break;
			case 30: s = "\"YR\" expected"; break;
			case 31: s = "\"TIL\" expected"; break;
			case 32: s = "\"WILE\" expected"; break;
			case 33: s = "\"O\" expected"; break;
			case 34: s = "\"RLY\" expected"; break;
			case 35: s = "\"YA\" expected"; break;
			case 36: s = "\"MEBBE\" expected"; break;
			case 37: s = "\"NO\" expected"; break;
			case 38: s = "\"WAI\" expected"; break;
			case 39: s = "\"OIC\" expected"; break;
			case 40: s = "\"WTF\" expected"; break;
			case 41: s = "\"OMG\" expected"; break;
			case 42: s = "\"OMGWTF\" expected"; break;
			case 43: s = "\"VISIBLE\" expected"; break;
			case 44: s = "\"INVISIBLE\" expected"; break;
			case 45: s = "\"!\" expected"; break;
			case 46: s = "\"NOW\" expected"; break;
			case 47: s = "\"OF\" expected"; break;
			case 48: s = "\"AN\" expected"; break;
			case 49: s = "\"MAEK\" expected"; break;
			case 50: s = "\"TROOF\" expected"; break;
			case 51: s = "\"NUMBR\" expected"; break;
			case 52: s = "\"NUMBAR\" expected"; break;
			case 53: s = "\"YARN\" expected"; break;
			case 54: s = "\"NOOB\" expected"; break;
			case 55: s = "\"WIN\" expected"; break;
			case 56: s = "\"FAIL\" expected"; break;
			case 57: s = "\"DUZ\" expected"; break;
			case 58: s = "\"IF\" expected"; break;
			case 59: s = "\"U\" expected"; break;
			case 60: s = "\"SAY\" expected"; break;
			case 61: s = "\"SO\" expected"; break;
			case 62: s = "\"FOUND\" expected"; break;
			case 63: s = "??? expected"; break;
			case 64: s = "invalid Statements"; break;
			case 65: s = "invalid Statement"; break;
			case 66: s = "invalid PrintStatement"; break;
			case 67: s = "invalid Expression"; break;
			case 68: s = "invalid SwitchLabel"; break;
			case 69: s = "invalid Const"; break;
			case 70: s = "invalid Typename"; break;
			case 71: s = "invalid Unary"; break;

			default: s = "error " + n; break;
		}
		cec.Add(new CompilerError(file, line, col, n.ToString(), s));
	}

	public void SemErr (string file, int line, int col, string s) {
		cec.Add(new CompilerError(file, line, col, "", s));
	}
	
	public void SemErr (string s) {
		cec.Add(new CompilerError(null, 0, 0, "", s));
	}
	
	public void Warning (string file, int line, int col, string s) {
		CompilerError ce = new CompilerError(file, line, col, "", s);
		ce.IsWarning = true;
		cec.Add(ce);
	}
	
	public void Warning(string s) {
		CompilerError ce = new CompilerError(null, 0, 0, "", s);
		ce.IsWarning = true;
		cec.Add(ce);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

}