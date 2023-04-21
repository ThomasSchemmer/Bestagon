public class Production {

    public Production(int[] InProduction) {
        _Production = InProduction;
    }

    public Production(int W, int S, int M, int F) {
        _Production = new int[] { W, S, M, F };
    }

    public Production() {
        _Production = new int[4];
    }

    public static Production operator +(Production A, Production B) {
        return new Production(A[0] + B[0], A[1] + B[1], A[2] + B[2], A[3] + B[3]);
    }

    public static Production operator -(Production A, Production B) {
        return new Production(A[0] - B[0], A[1] - B[1], A[2] - B[2], A[3] - B[3]);
    }

    public static bool operator <=(Production A, Production B) {
        return A[0] <= B[0] && A[1] <= B[1] && A[2] <= B[2] && A[3] <= B[3];
    }

    public static bool operator >=(Production A, Production B) {
        return A[0] >= B[0] && A[1] >= B[1] && A[2] >= B[2] && A[3] >= B[3];
    }

    public static Production operator* (Production A, int B) {
        return new Production(A[0] * B, A[1] * B, A[2] * B, A[3] * B);
    }

    public string GetDescription() {
        return "Wood: " + _Production[0] + "\tStone: " + _Production[1] + "\tMetal: " + _Production[2] + "\tFood: " + _Production[3];
    }

    public string GetDescription(int i) {
        switch (i) {
            case 0: return "Wood";
            case 1: return "Stone";
            case 2: return "Metal";
            case 3: return "Food";
            default: return "INVALID";
        }
    }

    public string GetShortDescription() {
        string ProductionText = string.Empty;
        for (int i = 0; i < _Production.Length; i++) {
            if (_Production[i] == 0)
                continue;

            ProductionText += _Production[i] + GetShortDescription(i) + " ";
        }

        return ProductionText;
    }

    public string GetShortDescription(int i) {
        switch (i) {
            case 0: return "W";
            case 1: return "S";
            case 2: return "M";
            case 3: return "F";
            default: return "INVALID";
        }
    }

    public int Wood {
        get { return _Production[0]; }
        set { _Production[0] = value; }
    }
    public int Stone {
        get { return _Production[1]; }
        set { _Production[1] = value; }
    }
    public int Metal {
        get { return _Production[2]; }
        set { _Production[2] = value; }
    }
    public int Food {
        get { return _Production[3]; }
        set { _Production[3] = value; }
    }

    public int this[int i] {
        get { return _Production[i]; }
        set { _Production[i] = value; }
    }

    private int[] _Production;
}
