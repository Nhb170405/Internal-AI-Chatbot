from fastapi import APIRouter

router = APIRouter(tags=["Health"])


@router.get("/health")
def health_check() -> dict[str, str]:
    # Muc tieu:
    # 1. Tra ve tin hieu de biet ai-service-python dang chay.
    # 2. Endpoint nay dung de test Python service doc lap truoc khi noi voi C#.
    return {
        "status": "ok",
        "service": "ai-service-python",
    }
