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
	const int maxT = 65;
	const int _comment = 66;
	const int _blockcomment = 67;

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
				if (la.kind == 66) {
				}
				if (la.kind == 67) {
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
			} else if (la.kind == 14) {
				Get();
				program.version = LOLCodeVersion.v1_1; 
			} else SynErr(66);
		}
		while (la.kind == 5) {
			Get();
		}
		Statements(out program.methods["Main"].statements);
		Expect(15);
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
			} else SynErr(67);
			while (la.kind == 5) {
				Get();
			}
		}
	}

	void CanHasStatement() {
		Expect(6);
		Expect(16);
		if (la.kind == 4) {
			Get();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(68);
		Expect(17);
	}

	void Statement(out Statement stat) {
		stat = null; 
		switch (la.kind) {
		case 18: {
			GimmehStatement(out stat);
			break;
		}
		case 28: {
			IHasAStatement(out stat);
			break;
		}
		case 8: {
			LoopStatement(out stat);
			break;
		}
		case 22: case 23: {
			BreakStatement(out stat);
			break;
		}
		case 27: {
			ContinueStatement(out stat);
			break;
		}
		case 32: case 33: case 34: case 35: {
			BinaryOpStatement(out stat);
			break;
		}
		case 37: {
			IzStatement(out stat);
			break;
		}
		case 41: {
			SwitchStatement(out stat);
			break;
		}
		case 45: case 46: {
			QuitStatement(out stat);
			break;
		}
		case 49: {
			AssignmentStatement(out stat);
			break;
		}
		case 47: case 48: {
			PrintStatement(out stat);
			break;
		}
		default: SynErr(69); break;
		}
	}

	void GimmehStatement(out Statement stat) {
		InputStatement ins = new InputStatement(GetPragma(la)); stat = ins; 
		Expect(18);
		if (la.kind == 19 || la.kind == 20 || la.kind == 21) {
			if (la.kind == 19) {
				Get();
			} else if (la.kind == 20) {
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
		Expect(28);
		Expect(16);
		Expect(29);
		Expect(1);
		vds.name = t.val; CreateLocal(vds.name); SetEndPragma(stat); 
		if (la.kind == 30) {
			Get();
			Expression(out vds.expression);
		}
	}

	void LoopStatement(out Statement stat) {
		LoopStatement ls = new LoopStatement(GetPragma(la)); stat = ls; 
		Expect(8);
		Expect(7);
		if (la.kind == 25) {
			Get();
		} else if (la.kind == 26) {
			Get();
		} else SynErr(70);
		Expect(1);
		ls.name = t.val; SetEndPragma(stat); BeginScope(); 
		while (la.kind == 5) {
			Get();
		}
		Statements(out ls.statements);
		if (la.kind == 31) {
			Get();
			if(program.version == LOLCodeVersion.IRCSPECZ) Warning("KTHX as a loop terminator is deprecated in favor of 'IM OUTTA YR <label>'"); 
		} else if (la.kind == 8) {
			Get();
			Expect(9);
			if (la.kind == 25) {
				Get();
			} else if (la.kind == 26) {
				Get();
			} else SynErr(71);
			Expect(1);
			if(t.val != ls.name) Error("Loop terminator label does not match loop label"); 
		} else SynErr(72);
		EndScope(); 
	}

	void BreakStatement(out Statement stat) {
		BreakStatement bs = new BreakStatement(GetPragma(la)); stat = bs; 
		if (la.kind == 22) {
			Get();
			if(program.version == LOLCodeVersion.IRCSPECZ) Warning("GTFO is deprecated in favor of ENUF"); 
		} else if (la.kind == 23) {
			Get();
			if (la.kind == 1 || la.kind == 24) {
				if (la.kind == 24) {
					Get();
					if (la.kind == 25) {
						Get();
					} else if (la.kind == 26) {
						Get();
					} else SynErr(73);
				}
				Expect(1);
				bs.label = t.val; 
			}
		} else SynErr(74);
		SetEndPragma(stat); 
	}

	void ContinueStatement(out Statement stat) {
		ContinueStatement cs = new ContinueStatement(GetPragma(la)); stat = cs; 
		Expect(27);
		if (la.kind == 1 || la.kind == 24) {
			if (la.kind == 24) {
				Get();
				if (la.kind == 25) {
					Get();
				} else if (la.kind == 26) {
					Get();
				} else SynErr(75);
			}
			Expect(1);
			cs.label = t.val; 
		}
		SetEndPragma(stat); 
	}

	void BinaryOpStatement(out Statement stat) {
		BinaryOpStatement bos = new BinaryOpStatement(GetPragma(la)); bos.amount = new PrimitiveExpression(GetPragma(la), 1); stat = bos; 
		if (la.kind == 32) {
			Get();
			bos.op = OpCodes.Add; 
		} else if (la.kind == 33) {
			Get();
			bos.op = OpCodes.Sub; 
		} else if (la.kind == 34) {
			Get();
			bos.op = OpCodes.Mul; 
		} else if (la.kind == 35) {
			Get();
			bos.op = OpCodes.Div; 
		} else SynErr(76);
		LValue(out bos.lval);
		Expect(36);
		if (StartOf(2)) {
			Expression(out bos.amount);
		}
		SetEndPragma(stat); 
	}

	void IzStatement(out Statement stat) {
		ConditionalStatement cs = new ConditionalStatement(GetPragma(la)); stat = cs; ConditionalStatement cur = cs; Statement st; 
		Expect(37);
		Expression(out cs.condition);
		if (la.kind == 17) {
			Get();
		} else if (la.kind == 5) {
			Get();
		} else SynErr(77);
		SetEndPragma(stat); 
		while (la.kind == 5) {
			Get();
		}
		if (la.kind == 38) {
			Get();
			while (la.kind == 5) {
				Get();
			}
		}
		BeginScope(); 
		Statements(out cs.trueStatements);
		EndScope(); 
		while (la.kind == 39) {
			Get();
			if (la.kind == 37) {
				Get();
			}
			cur.falseStatements = new ConditionalStatement(GetPragma(la)); cur = (ConditionalStatement)cur.falseStatements; 
			Expression(out cur.condition);
			Expect(17);
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.trueStatements);
			EndScope(); 
		}
		if (la.kind == 40) {
			Get();
			while (la.kind == 5) {
				Get();
			}
			BeginScope(); 
			Statements(out cur.falseStatements);
			EndScope(); 
		}
		Expect(31);
	}

	void SwitchStatement(out Statement stat) {
		SwitchStatement ss = new SwitchStatement(GetPragma(la)); stat = ss; Object label; Statement block; 
		Expect(41);
		if (la.kind == 37) {
			Get();
		}
		Expression(out ss.control);
		Expect(17);
		while (la.kind == 5) {
			Get();
		}
		while (la.kind == 42) {
			Get();
			Const(out label);
			if (la.kind == 43) {
				Get();
			}
			while (la.kind == 5) {
				Get();
			}
			Statements(out block);
			AddCase(ss, label, block); 
		}
		if (la.kind == 44) {
			Get();
			if (la.kind == 17) {
				Get();
			}
			while (la.kind == 5) {
				Get();
			}
			Statements(out ss.defaultCase);
		}
		Expect(31);
	}

	void QuitStatement(out Statement stat) {
		QuitStatement qs = new QuitStatement(GetPragma(la)); stat = qs; 
		if (la.kind == 45) {
			Get();
			qs.code = new PrimitiveExpression(GetPragma(la), 0); 
		} else if (la.kind == 46) {
			Get();
			if(program.version == LOLCodeVersion.IRCSPECZ) Warning("DIAF is deprecated. Use BYES instead."); qs.code = new PrimitiveExpression(GetPragma(la), 0); 
		} else SynErr(78);
		if (StartOf(2)) {
			Expression(out qs.code);
			if (StartOf(2)) {
				Expression(out qs.message);
			}
		}
		SetEndPragma(stat); 
	}

	void AssignmentStatement(out Statement stat) {
		AssignmentStatement ass = new AssignmentStatement(GetPragma(la)); stat = ass; 
		Expect(49);
		LValue(out ass.lval);
		Expect(50);
		Expression(out ass.rval);
		SetEndPragma(stat); 
	}

	void PrintStatement(out Statement stat) {
		PrintStatement ps = new PrintStatement(GetPragma(la)); stat = ps; 
		if (la.kind == 47) {
			Get();
		} else if (la.kind == 48) {
			Get();
			ps.stderr = true; 
		} else SynErr(79);
		Expression(out ps.message);
		if (la.kind == 43) {
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
		} else if (StartOf(2)) {
			ArrayIndex(out lv);
		} else SynErr(80);
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
		} else if (la.kind == 63) {
			Get();
			val = null; 
		} else SynErr(81);
	}

	void Unary(out Expression exp) {
		exp = null; Object val; 
		if ((la.kind == _intCon || la.kind == _stringCon) && !IsArrayIndex()) {
			Const(out val);
			exp = new PrimitiveExpression(GetPragma(t), val); SetEndPragma(exp); 
		} else if (StartOf(2)) {
			LValueExpression lve = new LValueExpression(GetPragma(la)); exp = lve; 
			LValue(out lve.lval);
			SetEndPragma(exp); 
		} else SynErr(82);
	}

	void AndExpression(out Expression exp, Expression left) {
		XorExpression(out exp, left);
		while (la.kind == 51) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.And; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			XorExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void XorExpression(out Expression exp, Expression left) {
		OrExpression(out exp, left);
		while (la.kind == 52) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.Xor; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			OrExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void OrExpression(out Expression exp, Expression left) {
		ComparisonExpression(out exp, left);
		while (la.kind == 53) {
			Get();
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.op = OpCodes.Or; ibs.left = exp; exp = ibs; 
			Unary(out ibs.right);
			ComparisonExpression(out ibs.right, ibs.right);
		}
		SetEndPragma(exp); 
	}

	void ComparisonExpression(out Expression exp, Expression left) {
		ArithmeticExpression(out exp, left);
		while (StartOf(3)) {
			ComparisonExpression ce = new ComparisonExpression(GetPragma(la)); ce.left = exp; exp = ce; 
			if (la.kind == 54) {
				Get();
				ce.op |= ComparisonOperator.Not; 
			}
			if (la.kind == 55) {
				Get();
				ce.op |= ComparisonOperator.GreaterThan; 
				if (la.kind == 56) {
					Get();
				}
			} else if (la.kind == 57) {
				Get();
				ce.op |= ComparisonOperator.LessThan; 
				if (la.kind == 56) {
					Get();
				}
			} else if (la.kind == 58) {
				Get();
				ce.op |= ComparisonOperator.Equal; 
			} else SynErr(83);
			Unary(out ce.right);
			ArithmeticExpression(out ce.right, ce.right);
		}
		SetEndPragma(exp); 
	}

	void ArithmeticExpression(out Expression exp, Expression left) {
		MultiplicationExpression(out exp, left);
		while (la.kind == 59 || la.kind == 60) {
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.left = exp; exp = ibs; 
			if (la.kind == 59) {
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
		while (la.kind == 61 || la.kind == 62) {
			IntegerBinaryExpression ibs = new IntegerBinaryExpression(GetPragma(la)); ibs.left = exp; exp = ibs; 
			if (la.kind == 61) {
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
		ArrayIndexLValue alv = new ArrayIndexLValue(GetPragma(la)); lv = alv; Object arg; 
		if (la.kind == 1) {
			Get();
			ReferenceLocal(t.val); alv.index = new LValueExpression(GetPragma(t), new VariableLValue(GetPragma(t), t.val)); SetEndPragma(alv.index); 
		} else if (la.kind == 2 || la.kind == 4 || la.kind == 63) {
			Const(out arg);
			alv.index = new PrimitiveExpression(GetPragma(t), arg); SetEndPragma(alv.index); 
		} else SynErr(84);
		Expect(7);
		Expect(64);
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
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,T,T, x,x,x,T, T,x,x,x, T,T,T,T, x,T,x,x, x,T,x,x, x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,T,x, x,x,x,x, x,x,x}

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
			case 10: s = "\"HAI\" expected"; break;
			case 11: s = "\"TO\" expected"; break;
			case 12: s = "\"1.0\" expected"; break;
			case 13: s = "\"IRCSPECZ\" expected"; break;
			case 14: s = "\"1.1\" expected"; break;
			case 15: s = "\"KTHXBYE\" expected"; break;
			case 16: s = "\"HAS\" expected"; break;
			case 17: s = "\"?\" expected"; break;
			case 18: s = "\"GIMMEH\" expected"; break;
			case 19: s = "\"LINE\" expected"; break;
			case 20: s = "\"WORD\" expected"; break;
			case 21: s = "\"LETTAR\" expected"; break;
			case 22: s = "\"GTFO\" expected"; break;
			case 23: s = "\"ENUF\" expected"; break;
			case 24: s = "\"OV\" expected"; break;
			case 25: s = "\"YR\" expected"; break;
			case 26: s = "\"UR\" expected"; break;
			case 27: s = "\"MOAR\" expected"; break;
			case 28: s = "\"I\" expected"; break;
			case 29: s = "\"A\" expected"; break;
			case 30: s = "\"ITZ\" expected"; break;
			case 31: s = "\"KTHX\" expected"; break;
			case 32: s = "\"UPZ\" expected"; break;
			case 33: s = "\"NERFZ\" expected"; break;
			case 34: s = "\"TIEMZD\" expected"; break;
			case 35: s = "\"OVARZ\" expected"; break;
			case 36: s = "\"!!\" expected"; break;
			case 37: s = "\"IZ\" expected"; break;
			case 38: s = "\"YARLY\" expected"; break;
			case 39: s = "\"MEBBE\" expected"; break;
			case 40: s = "\"NOWAI\" expected"; break;
			case 41: s = "\"WTF\" expected"; break;
			case 42: s = "\"OMG\" expected"; break;
			case 43: s = "\"!\" expected"; break;
			case 44: s = "\"OMGWTF\" expected"; break;
			case 45: s = "\"BYES\" expected"; break;
			case 46: s = "\"DIAF\" expected"; break;
			case 47: s = "\"VISIBLE\" expected"; break;
			case 48: s = "\"INVISIBLE\" expected"; break;
			case 49: s = "\"LOL\" expected"; break;
			case 50: s = "\"R\" expected"; break;
			case 51: s = "\"AND\" expected"; break;
			case 52: s = "\"XOR\" expected"; break;
			case 53: s = "\"OR\" expected"; break;
			case 54: s = "\"NOT\" expected"; break;
			case 55: s = "\"BIGR\" expected"; break;
			case 56: s = "\"THAN\" expected"; break;
			case 57: s = "\"SMALR\" expected"; break;
			case 58: s = "\"LIEK\" expected"; break;
			case 59: s = "\"UP\" expected"; break;
			case 60: s = "\"NERF\" expected"; break;
			case 61: s = "\"TIEMZ\" expected"; break;
			case 62: s = "\"OVAR\" expected"; break;
			case 63: s = "\"NOOB\" expected"; break;
			case 64: s = "\"MAH\" expected"; break;
			case 65: s = "??? expected"; break;
			case 66: s = "invalid LOLCode"; break;
			case 67: s = "invalid Statements"; break;
			case 68: s = "invalid CanHasStatement"; break;
			case 69: s = "invalid Statement"; break;
			case 70: s = "invalid LoopStatement"; break;
			case 71: s = "invalid LoopStatement"; break;
			case 72: s = "invalid LoopStatement"; break;
			case 73: s = "invalid BreakStatement"; break;
			case 74: s = "invalid BreakStatement"; break;
			case 75: s = "invalid ContinueStatement"; break;
			case 76: s = "invalid BinaryOpStatement"; break;
			case 77: s = "invalid IzStatement"; break;
			case 78: s = "invalid QuitStatement"; break;
			case 79: s = "invalid PrintStatement"; break;
			case 80: s = "invalid LValue"; break;
			case 81: s = "invalid Const"; break;
			case 82: s = "invalid Unary"; break;
			case 83: s = "invalid ComparisonExpression"; break;
			case 84: s = "invalid ArrayIndex"; break;

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