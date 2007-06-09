using System.Collections.Generic;
using System.Reflection.Emit;

using System;
using System.CodeDom.Compiler;

namespace notdot.LOLCode {



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
	const int maxT = 62;
	const int _comment = 63;

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
				if (la.kind == 63) {
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
		Expect(10);
		if (la.kind == 11) {
			Get();
			if (la.kind == 12) {
				Get();
				program.version = LOLCodeVersion.v1_0; 
			} else if (la.kind == 13) {
				Get();
				program.version = LOLCodeVersion.IRCSPECZ; 
			} else SynErr(63);
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
			} else SynErr(64);
			while (la.kind == 5) {
				Get();
			}
		}
	}

	void CanHasStatement() {
		Expect(6);
		Expect(15);
		if (la.kind == 4) {
			Get();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(65);
		Expect(16);
	}

	void Statement(out Statement stat) {
		stat = null; 
		switch (la.kind) {
		case 17: {
			GimmehStatement(out stat);
			break;
		}
		case 27: {
			IHasAStatement(out stat);
			break;
		}
		case 8: {
			LoopStatement(out stat);
			break;
		}
		case 21: case 22: {
			BreakStatement(out stat);
			break;
		}
		case 26: {
			ContinueStatement(out stat);
			break;
		}
		case 30: case 31: case 32: case 33: {
			BinaryOpStatement(out stat);
			break;
		}
		case 35: {
			IzStatement(out stat);
			break;
		}
		case 39: {
			SwitchStatement(out stat);
			break;
		}
		case 43: case 44: {
			QuitStatement(out stat);
			break;
		}
		case 47: {
			AssignmentStatement(out stat);
			break;
		}
		case 45: case 46: {
			PrintStatement(out stat);
			break;
		}
		default: SynErr(66); break;
		}
	}

	void GimmehStatement(out Statement stat) {
		InputStatement ins = new InputStatement(GetPragma(la)); stat = ins; 
		Expect(17);
		if (la.kind == 18 || la.kind == 19 || la.kind == 20) {
			if (la.kind == 18) {
				Get();
			} else if (la.kind == 19) {
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

	void IHasAStatement(out Statement stat) {
		VariableDeclarationStatement vds = new VariableDeclarationStatement(GetPragma(la)); stat = vds; 
		Expect(27);
		Expect(15);
		Expect(28);
		Expect(1);
		vds.name = t.val; CreateLocal(vds.name); SetEndPragma(stat); 
	}

	void LoopStatement(out Statement stat) {
		LoopStatement ls = new LoopStatement(GetPragma(la)); stat = ls; 
		Expect(8);
		Expect(7);
		if (la.kind == 24) {
			Get();
		} else if (la.kind == 25) {
			Get();
		} else SynErr(67);
		Expect(1);
		ls.name = t.val; SetEndPragma(stat); BeginScope(); 
		while (la.kind == 5) {
			Get();
		}
		Statements(out ls.statements);
		if (la.kind == 29) {
			Get();
			Warning("KTHX as a loop terminator is deprecated in favor of 'IM OUTTA YR <label>'"); 
		} else if (la.kind == 8) {
			Get();
			Expect(9);
			if (la.kind == 24) {
				Get();
			} else if (la.kind == 25) {
				Get();
			} else SynErr(68);
			Expect(1);
			if(t.val != ls.name) Error("Loop terminator label does not match loop label"); 
		} else SynErr(69);
		EndScope(); 
	}

	void BreakStatement(out Statement stat) {
		BreakStatement bs = new BreakStatement(GetPragma(la)); stat = bs; 
		if (la.kind == 21) {
			Get();
			Warning("GTFO is deprecated in favor of ENUF"); 
		} else if (la.kind == 22) {
			Get();
			if (la.kind == 1 || la.kind == 23) {
				if (la.kind == 23) {
					Get();
					if (la.kind == 24) {
						Get();
					} else if (la.kind == 25) {
						Get();
					} else SynErr(70);
				}
				Expect(1);
				bs.label = t.val; 
			}
		} else SynErr(71);
		SetEndPragma(stat); 
	}

	void ContinueStatement(out Statement stat) {
		ContinueStatement cs = new ContinueStatement(GetPragma(la)); stat = cs; 
		Expect(26);
		if (la.kind == 1 || la.kind == 23) {
			if (la.kind == 23) {
				Get();
				if (la.kind == 24) {
					Get();
				} else if (la.kind == 25) {
					Get();
				} else SynErr(72);
			}
			Expect(1);
			cs.label = t.val; 
		}
		SetEndPragma(stat); 
	}

	void BinaryOpStatement(out Statement stat) {
		BinaryOpStatement bos = new BinaryOpStatement(GetPragma(la)); bos.amount = new PrimitiveExpression(GetPragma(la), 1); stat = bos; 
		if (la.kind == 30) {
			Get();
			bos.op = OpCodes.Add; 
		} else if (la.kind == 31) {
			Get();
			bos.op = OpCodes.Sub; 
		} else if (la.kind == 32) {
			Get();
			bos.op = OpCodes.Mul; 
		} else if (la.kind == 33) {
			Get();
			bos.op = OpCodes.Div; 
		} else SynErr(73);
		LValue(out bos.lval);
		Expect(34);
		if (la.kind == 1 || la.kind == 2 || la.kind == 4) {
			Expression(out bos.amount);
		}
		SetEndPragma(stat); 
	}

	void IzStatement(out Statement stat) {
		ConditionalStatement cs = new ConditionalStatement(GetPragma(la)); stat = cs; ConditionalStatement cur = cs; Statement st; 
		Expect(35);
		Expression(out cs.condition);
		if (la.kind == 16) {
			Get();
		} else if (la.kind == 5) {
			Get();
		} else SynErr(74);
		SetEndPragma(stat); 
		while (la.kind == 5) {
			Get();
		}
		if (la.kind == 36) {
			Get();
			while (la.kind == 5) {
				Get();
			}
		}
		BeginScope(); 
		Statements(out cs.trueStatements);
		EndScope(); 
		while (la.kind == 37) {
			Get();
			if (la.kind == 35) {
				Get();
			}
			cur.falseStatements = new ConditionalStatement(GetPragma(la)); cur = (ConditionalStatement)cur.falseStatements; 
			Expression(out cur.condition);
			Expect(16);
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.trueStatements);
			EndScope(); 
		}
		if (la.kind == 38) {
			Get();
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.falseStatements);
			EndScope(); 
		}
		Expect(29);
	}

	void SwitchStatement(out Statement stat) {
		SwitchStatement ss = new SwitchStatement(GetPragma(la)); stat = ss; Object label; Statement block; 
		Expect(39);
		if (la.kind == 35) {
			Get();
		}
		Expression(out ss.control);
		Expect(16);
		while (la.kind == 5) {
			Get();
		}
		while (la.kind == 40) {
			Get();
			Const(out label);
			if (la.kind == 41) {
				Get();
			}
			while (la.kind == 5) {
				Get();
			}
			Statements(out block);
			AddCase(ss, label, block); 
		}
		if (la.kind == 42) {
			Get();
			if (la.kind == 16) {
				Get();
			}
			while (la.kind == 5) {
				Get();
			}
			Statements(out ss.defaultCase);
		}
		Expect(29);
	}

	void QuitStatement(out Statement stat) {
		QuitStatement qs = new QuitStatement(GetPragma(la)); stat = qs; 
		if (la.kind == 43) {
			Get();
			qs.code = new PrimitiveExpression(GetPragma(la), 0); 
		} else if (la.kind == 44) {
			Get();
			Warning("DIAF is deprecated. Use BYES instead."); qs.code = new PrimitiveExpression(GetPragma(la), 0); 
		} else SynErr(75);
		if (la.kind == 1 || la.kind == 2 || la.kind == 4) {
			Expression(out qs.code);
			if (la.kind == 1 || la.kind == 2 || la.kind == 4) {
				Expression(out qs.message);
			}
		}
		SetEndPragma(stat); 
	}

	void AssignmentStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); stat = ass; 
		Expect(47);
		LValue(out ass.lval);
		Expect(48);
		Expression(out ass.rval);
		SetEndPragma(stat); 
	}

	void PrintStatement(out Statement stat) {
		PrintStatement ps = new PrintStatement(GetPragma(la)); stat = ps; 
		if (la.kind == 45) {
			Get();
		} else if (la.kind == 46) {
			Get();
			ps.stderr = true; 
		} else SynErr(76);
		Expression(out ps.message);
		if (la.kind == 41) {
			Get();
			ps.newline = false; 
		}
		SetEndPragma(stat); 
	}

	void LValue(out LValue lv) {
		lv = null; 
		if (!IsArrayIndex()) {
			Expect(1);
			ReferenceLocal(t.val); lv = new VariableLValue(GetPragma(t), t.val); SetEndPragma(lv); 
		} else if (la.kind == 1 || la.kind == 2) {
			ArrayIndex(out lv);
		} else SynErr(77);
	}

	void Expression(out Expression exp) {
		Expression left; 
		Unary(out left);
		AndExpression(out exp, left);
		SetEndPragma(exp); 
	}

	void Const(out object val) {
		val = null; 
		if (la.kind == 2) {
			Get();
			val = int.Parse(t.val); 
		} else if (la.kind == 4) {
			Get();
			val = UnescapeString(t.val); 
		} else SynErr(78);
	}

	void Unary(out Expression exp) {
		exp = null; Object val; 
		if ((la.kind == _intCon || la.kind == _stringCon) && !IsArrayIndex()) {
			Const(out val);
			exp = new PrimitiveExpression(GetPragma(t), val); SetEndPragma(exp); 
		} else if (la.kind == 1 || la.kind == 2) {
			LValueExpression lve = new LValueExpression(GetPragma(la)); exp = lve; 
			LValue(out lve.lval);
			SetEndPragma(exp); 
		} else SynErr(79);
	}

	void AndExpression(out Expression exp, Expression left) {
		XorExpression(out exp, left);
		while (la.kind == 49) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.And; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			XorExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void XorExpression(out Expression exp, Expression left) {
		OrExpression(out exp, left);
		while (la.kind == 50) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.Xor; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			OrExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void OrExpression(out Expression exp, Expression left) {
		ComparisonExpression(out exp, left);
		while (la.kind == 51) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.Or; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			ComparisonExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void ComparisonExpression(out Expression exp, Expression left) {
		ArithmeticExpression(out exp, left);
		while (StartOf(2)) {
			ComparisonExpression ce = new ComparisonExpression(GetPragma(la)); ce.left = exp; exp = ce; 
			if (la.kind == 52) {
				Get();
				ce.op |= ComparisonOperator.Not; 
			}
			if (la.kind == 53) {
				Get();
				ce.op |= ComparisonOperator.GreaterThan; 
				if (la.kind == 54) {
					Get();
				}
			} else if (la.kind == 55) {
				Get();
				ce.op |= ComparisonOperator.LessThan; 
				if (la.kind == 54) {
					Get();
				}
			} else if (la.kind == 56) {
				Get();
				ce.op |= ComparisonOperator.Equal; 
			} else SynErr(80);
			Unary(out ce.right);
			ArithmeticExpression(out ce.right, ce.right);
		}
		SetEndPragma(exp); 
	}

	void ArithmeticExpression(out Expression exp, Expression left) {
		MultiplicationExpression(out exp, left);
		while (la.kind == 57 || la.kind == 58) {
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.left = exp; exp = ibs; 
			if (la.kind == 57) {
				Get();
				ibs.op = OpCodes.Add; 
			} else {
				Get();
				ibs.op = OpCodes.Sub; 
			}
			Unary(out ibs.right);
			MultiplicationExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void MultiplicationExpression(out Expression exp, Expression left) {
		exp = left; 
		while (la.kind == 59 || la.kind == 60) {
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.left = exp; exp = ibs; 
			if (la.kind == 59) {
				Get();
				ibs.op = OpCodes.Mul; 
			} else {
				Get();
				ibs.op = OpCodes.Div; 
			}
			Unary(out ibs.right);
		}
		SetEndPragma(exp); 
	}

	void ArrayIndex(out LValue lv) {
		ArrayIndexLValue alv = new ArrayIndexLValue(GetPragma(la)); lv = alv; 
		if (la.kind == 1) {
			Get();
			ReferenceLocal(t.val); alv.index = new LValueExpression(GetPragma(t), new VariableLValue(GetPragma(t), t.val)); SetEndPragma(alv.index); 
		} else if (la.kind == 2) {
			Get();
			alv.index = new PrimitiveExpression(GetPragma(t), int.Parse(t.val)); SetEndPragma(alv.index); 
		} else SynErr(81);
		Expect(7);
		Expect(61);
		LValue(out alv.lval);
		SetEndPragma(alv); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		LOLCode();

    Expect(0);
	}
	
	bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,T,T,x, x,x,T,T, x,x,T,T, T,T,x,T, x,x,x,T, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,x, x,x,x,x}

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
			case 10: s = "\"hai\" expected"; break;
			case 11: s = "\"to\" expected"; break;
			case 12: s = "\"1.0\" expected"; break;
			case 13: s = "\"ircspecz\" expected"; break;
			case 14: s = "\"kthxbye\" expected"; break;
			case 15: s = "\"has\" expected"; break;
			case 16: s = "\"?\" expected"; break;
			case 17: s = "\"gimmeh\" expected"; break;
			case 18: s = "\"line\" expected"; break;
			case 19: s = "\"word\" expected"; break;
			case 20: s = "\"lettar\" expected"; break;
			case 21: s = "\"gtfo\" expected"; break;
			case 22: s = "\"enuf\" expected"; break;
			case 23: s = "\"ov\" expected"; break;
			case 24: s = "\"yr\" expected"; break;
			case 25: s = "\"ur\" expected"; break;
			case 26: s = "\"moar\" expected"; break;
			case 27: s = "\"i\" expected"; break;
			case 28: s = "\"a\" expected"; break;
			case 29: s = "\"kthx\" expected"; break;
			case 30: s = "\"upz\" expected"; break;
			case 31: s = "\"nerfz\" expected"; break;
			case 32: s = "\"tiemzd\" expected"; break;
			case 33: s = "\"ovarz\" expected"; break;
			case 34: s = "\"!!\" expected"; break;
			case 35: s = "\"iz\" expected"; break;
			case 36: s = "\"yarly\" expected"; break;
			case 37: s = "\"mebbe\" expected"; break;
			case 38: s = "\"nowai\" expected"; break;
			case 39: s = "\"wtf\" expected"; break;
			case 40: s = "\"omg\" expected"; break;
			case 41: s = "\"!\" expected"; break;
			case 42: s = "\"omgwtf\" expected"; break;
			case 43: s = "\"byes\" expected"; break;
			case 44: s = "\"diaf\" expected"; break;
			case 45: s = "\"visible\" expected"; break;
			case 46: s = "\"invisible\" expected"; break;
			case 47: s = "\"lol\" expected"; break;
			case 48: s = "\"r\" expected"; break;
			case 49: s = "\"and\" expected"; break;
			case 50: s = "\"xor\" expected"; break;
			case 51: s = "\"or\" expected"; break;
			case 52: s = "\"not\" expected"; break;
			case 53: s = "\"bigr\" expected"; break;
			case 54: s = "\"than\" expected"; break;
			case 55: s = "\"smalr\" expected"; break;
			case 56: s = "\"liek\" expected"; break;
			case 57: s = "\"up\" expected"; break;
			case 58: s = "\"nerf\" expected"; break;
			case 59: s = "\"tiemz\" expected"; break;
			case 60: s = "\"ovar\" expected"; break;
			case 61: s = "\"mah\" expected"; break;
			case 62: s = "??? expected"; break;
			case 63: s = "invalid LOLCode"; break;
			case 64: s = "invalid Statements"; break;
			case 65: s = "invalid CanHasStatement"; break;
			case 66: s = "invalid Statement"; break;
			case 67: s = "invalid LoopStatement"; break;
			case 68: s = "invalid LoopStatement"; break;
			case 69: s = "invalid LoopStatement"; break;
			case 70: s = "invalid BreakStatement"; break;
			case 71: s = "invalid BreakStatement"; break;
			case 72: s = "invalid ContinueStatement"; break;
			case 73: s = "invalid BinaryOpStatement"; break;
			case 74: s = "invalid IzStatement"; break;
			case 75: s = "invalid QuitStatement"; break;
			case 76: s = "invalid PrintStatement"; break;
			case 77: s = "invalid LValue"; break;
			case 78: s = "invalid Const"; break;
			case 79: s = "invalid Unary"; break;
			case 80: s = "invalid ComparisonExpression"; break;
			case 81: s = "invalid ArrayIndex"; break;

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