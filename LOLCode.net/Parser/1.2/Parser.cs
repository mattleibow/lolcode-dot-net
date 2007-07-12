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
	const int maxT = 51;
	const int _comment = 52;
	const int _blockcomment = 53;
	const int _continuation = 54;

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
				if (la.kind == 52) {
				}
				if (la.kind == 53) {
				}
				if (la.kind == 54) {
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
		Expect(11);
		if (la.kind == 12) {
			Get();
			Expect(13);
			program.version = LOLCodeVersion.v1_2; 
		}
		while (la.kind == 5) {
			Get();
		}
		Statements(out program.methods["Main"].statements);
		Expect(14);
		while (la.kind == 5) {
			Get();
		}
	}

	void Statements(out Statement stat) {
		BlockStatement bs = new BlockStatement(GetPragma(t)); stat = bs; 
		Statement s; 
		while ((StartOf(1) || la.kind == _can) && scanner.Peek().kind != _outta) {
			if (la.kind == 6) {
				CanHasStatement();
			} else if (StartOf(1)) {
				Statement(out s);
				bs.statements.Add(s); 
			} else SynErr(52);
			while (la.kind == 5) {
				Get();
			}
		}
	}

	void CanHasStatement() {
		StringBuilder sb = new StringBuilder(); 
		Expect(6);
		Expect(16);
		Expect(1);
		sb.Append(t.val); 
		while (la.kind == 20) {
			Get();
			Expect(1);
			sb.Append('.'); sb.Append(t.val); 
		}
		Expect(21);
		if(!program.ImportLibrary(sb.ToString())) Error(string.Format("Library \"{0}\" not found.", sb.ToString())); 
	}

	void Statement(out Statement stat) {
		stat = null; 
		switch (la.kind) {
		case 15: {
			IHasAStatement(out stat);
			break;
		}
		case 1: {
			AssignmentStatement(out stat);
			break;
		}
		case 22: {
			GimmehStatement(out stat);
			break;
		}
		case 8: {
			LoopStatement(out stat);
			break;
		}
		case 26: {
			BreakStatement(out stat);
			break;
		}
		case 27: {
			ContinueStatement(out stat);
			break;
		}
		case 32: {
			OrlyStatement(out stat);
			break;
		}
		case 39: {
			SwitchStatement(out stat);
			break;
		}
		case 43: case 44: {
			PrintStatement(out stat);
			break;
		}
		default: SynErr(53); break;
		}
	}

	void IHasAStatement(out Statement stat) {
		VariableDeclarationStatement vds = new VariableDeclarationStatement(GetPragma(la)); stat = vds; 
		Expect(15);
		Expect(16);
		Expect(17);
		Expect(1);
		vds.var = DeclareVariable(t.val); SetEndPragma(stat); 
		if (la.kind == 18) {
			Get();
			Expression(out vds.expression);
		}
	}

	void AssignmentStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); stat = ass; 
		LValue(out ass.lval);
		Expect(19);
		Expression(out ass.rval);
		SetEndPragma(stat); 
	}

	void GimmehStatement(out Statement stat) {
		InputStatement ins = new InputStatement(GetPragma(la)); stat = ins; 
		Expect(22);
		if (la.kind == 23 || la.kind == 24 || la.kind == 25) {
			if (la.kind == 23) {
				Get();
			} else if (la.kind == 24) {
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
		if (la.kind == 28) {
			Get();
		} else if (la.kind == 29) {
			Get();
		} else SynErr(54);
		Expect(1);
		ls.name = t.val; SetEndPragma(stat); BeginScope(); 
		if (la.kind == 1) {
			Get();
			ls.operation = GetFunction(t.val); 
			Expect(28);
			Expect(1);
			ls.loopvar = CreateLoopVariable(t.val); 
			if (la.kind == 30 || la.kind == 31) {
				if (la.kind == 30) {
					Get();
					ls.type = LoopType.Until; 
				} else {
					Get();
					ls.type = LoopType.While; 
					Expression(out ls.condition);
				}
			}
		}
		Expect(5);
		while (la.kind == 5) {
			Get();
		}
		Statements(out ls.statements);
		Expect(8);
		Expect(9);
		if (la.kind == 28) {
			Get();
		} else if (la.kind == 29) {
			Get();
		} else SynErr(55);
		Expect(1);
		RemoveLoopVariable(ls.loopvar); if(t.val != ls.name) Error("Loop terminator label does not match loop label"); EndScope(); 
	}

	void BreakStatement(out Statement stat) {
		BreakStatement bs = new BreakStatement(GetPragma(la)); stat = bs; 
		Expect(26);
		if (la.kind == 1) {
			Get();
			bs.label = t.val; 
		}
		Expect(5);
		SetEndPragma(stat); 
	}

	void ContinueStatement(out Statement stat) {
		ContinueStatement cs = new ContinueStatement(GetPragma(la)); stat = cs; 
		Expect(27);
		if (la.kind == 1) {
			Get();
			cs.label = t.val; 
		}
		Expect(5);
		SetEndPragma(stat); 
	}

	void OrlyStatement(out Statement stat) {
		ConditionalStatement cs = new ConditionalStatement(GetPragma(la)); stat = cs; ConditionalStatement cur = cs; Statement st; Expression e; cs.condition = new VariableLValue(GetPragma(la), GetVariable("IT")); 
		Expect(32);
		Expect(33);
		Expect(21);
		while (la.kind == 5) {
			Get();
		}
		if (la.kind == 34) {
			Get();
			Expect(33);
			while (la.kind == 5) {
				Get();
			}
		}
		BeginScope(); 
		Statements(out cs.trueStatements);
		EndScope(); 
		while (la.kind == 35) {
			Get();
			cur.falseStatements = new ConditionalStatement(GetPragma(la)); 
			Expression(out e);
			while (la.kind == 5) {
				Get();
			}
			(cur.falseStatements as ConditionalStatement).condition = e; cur = (ConditionalStatement)cur.falseStatements; BeginScope(); 
			Statements(out cur.trueStatements);
			EndScope(); 
		}
		if (la.kind == 36) {
			Get();
			Expect(37);
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.falseStatements);
			EndScope(); 
		}
		Expect(38);
	}

	void SwitchStatement(out Statement stat) {
		SwitchStatement ss = new SwitchStatement(GetPragma(la)); ss.control = new VariableLValue(GetPragma(la), GetVariable("IT")); stat = ss; Object label; Statement block; 
		Expect(39);
		Expect(21);
		while (la.kind == 5) {
			Get();
		}
		while (la.kind == 40) {
			Get();
			Const(out label);
			while (la.kind == 5) {
				Get();
			}
			Statements(out block);
			AddCase(ss, label, block); 
		}
		if (la.kind == 41) {
			Get();
			while (la.kind == 5) {
				Get();
			}
			Statements(out ss.defaultCase);
		}
		Expect(42);
	}

	void PrintStatement(out Statement stat) {
		PrintStatement ps = new PrintStatement(GetPragma(la)); ps.message = new FunctionExpression(GetPragma(la), GetFunction("SMOOSH")); stat = ps; Expression e;
		if (la.kind == 43) {
			Get();
		} else if (la.kind == 44) {
			Get();
			ps.stderr = true; 
		} else SynErr(56);
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

	void Expression(out Expression exp) {
		exp = null; 
		if (IsFunction(la.val)) {
			FunctionExpression(out exp);
		} else if (StartOf(2)) {
			Unary(out exp);
		} else SynErr(57);
	}

	void LValue(out LValue lv) {
		lv = null; 
		Expect(1);
		lv = new VariableLValue(GetPragma(t), GetVariable(t.val)); SetEndPragma(lv); 
	}

	void Const(out object val) {
		val = null; 
		if (la.kind == 2) {
			Get();
			val = int.Parse(t.val); 
		} else if (la.kind == 3) {
			Get();
			val = float.Parse(t.val); 
		} else if (la.kind == 48) {
			Get();
			val = null; 
		} else if (la.kind == 49) {
			Get();
			val = true; 
		} else if (la.kind == 50) {
			Get();
			val = false; 
		} else SynErr(58);
	}

	void FunctionExpression(out Expression exp) {
		FunctionExpression fe = new FunctionExpression(GetPragma(la)); exp = fe; Expression e2; 
		Expect(1);
		fe.func = GetFunction(t.val); int argsLeft = fe.func.Arity; 
		if (la.kind == 46) {
			Get();
		}
		if (argsLeft > 0 || (fe.func.IsVariadic && la.kind != _mkay)) {
			Expression(out e2);
			fe.arguments.Add(e2); argsLeft--; 
			while (argsLeft > 0 || (fe.func.IsVariadic && la.kind != _mkay)) {
				if (la.kind == 47) {
					Get();
				}
				Expression(out e2);
				fe.arguments.Add(e2); argsLeft--; 
			}
		}
		if (fe.func.IsVariadic) {
			Expect(10);
		}
	}

	void Unary(out Expression exp) {
		exp = null; Object val; LValue lv; 
		if (StartOf(3)) {
			Const(out val);
			exp = new PrimitiveExpression(GetPragma(t), val); SetEndPragma(exp); 
		} else if (la.kind == 4) {
			StringExpression(out exp);
		} else if (la.kind == 1) {
			LValue(out lv);
			exp = lv; SetEndPragma(exp); 
		} else SynErr(59);
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
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,x, T,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,T,T, x,x,x,x, T,x,x,x, x,x,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x},
		{x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x}

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
			case 11: s = "\"HAI\" expected"; break;
			case 12: s = "\"TO\" expected"; break;
			case 13: s = "\"1.2\" expected"; break;
			case 14: s = "\"KTHXBYE\" expected"; break;
			case 15: s = "\"I\" expected"; break;
			case 16: s = "\"HAS\" expected"; break;
			case 17: s = "\"A\" expected"; break;
			case 18: s = "\"ITZ\" expected"; break;
			case 19: s = "\"R\" expected"; break;
			case 20: s = "\".\" expected"; break;
			case 21: s = "\"?\" expected"; break;
			case 22: s = "\"GIMMEH\" expected"; break;
			case 23: s = "\"LINE\" expected"; break;
			case 24: s = "\"WORD\" expected"; break;
			case 25: s = "\"LETTAR\" expected"; break;
			case 26: s = "\"GTFO\" expected"; break;
			case 27: s = "\"MOAR\" expected"; break;
			case 28: s = "\"YR\" expected"; break;
			case 29: s = "\"UR\" expected"; break;
			case 30: s = "\"TIL\" expected"; break;
			case 31: s = "\"WILE\" expected"; break;
			case 32: s = "\"O\" expected"; break;
			case 33: s = "\"RLY\" expected"; break;
			case 34: s = "\"YA\" expected"; break;
			case 35: s = "\"MEBBE\" expected"; break;
			case 36: s = "\"NO\" expected"; break;
			case 37: s = "\"WAI\" expected"; break;
			case 38: s = "\"OIC\" expected"; break;
			case 39: s = "\"WTF\" expected"; break;
			case 40: s = "\"OMG\" expected"; break;
			case 41: s = "\"OMGWTF\" expected"; break;
			case 42: s = "\"KTHX\" expected"; break;
			case 43: s = "\"VISIBLE\" expected"; break;
			case 44: s = "\"INVISIBLE\" expected"; break;
			case 45: s = "\"!\" expected"; break;
			case 46: s = "\"OF\" expected"; break;
			case 47: s = "\"AN\" expected"; break;
			case 48: s = "\"NOOB\" expected"; break;
			case 49: s = "\"WIN\" expected"; break;
			case 50: s = "\"FAIL\" expected"; break;
			case 51: s = "??? expected"; break;
			case 52: s = "invalid Statements"; break;
			case 53: s = "invalid Statement"; break;
			case 54: s = "invalid LoopStatement"; break;
			case 55: s = "invalid LoopStatement"; break;
			case 56: s = "invalid PrintStatement"; break;
			case 57: s = "invalid Expression"; break;
			case 58: s = "invalid Const"; break;
			case 59: s = "invalid Unary"; break;

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