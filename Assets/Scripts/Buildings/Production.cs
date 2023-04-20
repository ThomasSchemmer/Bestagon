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

    public int this[int i] {
        get { return _Production[i]; }
        set { _Production[i] = value; }
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

    private int[] _Production;
}
