namespace backend_dotnet.Modules.Safety;

public sealed class PromptSafetyService
{
    public PromptSafetyResult CheckUserMessage(string message)
    {
        // Bai tap Milestone 16:
        // 1. Neu message rong thi return IsAllowed=false.
        // 2. Normalize message ve lowercase de check pattern.
        // 3. Detect cac pattern ro rang:
        //    - "ignore previous instructions"
        //    - "show system prompt"
        //    - "reveal api key"
        //    - "bypass permission"
        // 4. Neu nguy hiem thi return IsAllowed=false voi SafeMessage ro rang.
        // 5. Neu khong match thi IsAllowed=true.
        //
        // Luu y:
        // - Day chi la baseline rule-based.
        // - Bao ve chinh cua RAG van la permission filter va context duoc phep.
        return new PromptSafetyResult
        {
            IsAllowed = true
        };
    }
}
