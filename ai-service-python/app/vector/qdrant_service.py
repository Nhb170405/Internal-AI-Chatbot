from qdrant_client import QdrantClient
from qdrant_client.http import models
from app.config.settings import AppSettings
from app.vector.vector_models import VectorPointInput, VectorSearchResult


class QdrantService:
    def __init__(self, settings: AppSettings):
        # Bai tap Milestone 7:
        # 1. Luu settings.
        # 2. Tao QdrantClient(url=settings.qdrant_url).
        
        self._settings = settings
        self._client = QdrantClient(url=settings.qdrant_url)

    def ensure_collection(self, vector_size: int) -> None:
        exists = self._client.collection_exists(
            collection_name=self._settings.qdrant_collection
        )

        if exists:
            collection_info = self._client.get_collection(
                collection_name=self._settings.qdrant_collection
            )

            existing_size = collection_info.config.params.vectors.size

            if existing_size != vector_size:
                raise ValueError(
                    f"Collection vector size mismatch. Existing={existing_size}, expected={vector_size}."
                )

            return

        self._client.create_collection(
            collection_name=self._settings.qdrant_collection,
            vectors_config=models.VectorParams(
                size=vector_size,
                distance=models.Distance.COSINE,
            ),
        )

    def upsert_points(self, points: list[VectorPointInput]) -> None:
        # Bai tap Milestone 7:
        # 1. Validate points khong rong.
        # 2. Map VectorPointInput thanh qdrant models.PointStruct.
        # 3. Goi self._client.upsert(collection_name=..., points=...).
        #
        # Luu y:
        # - point_id nen on dinh theo chunkId de re-index khong duplicate.
        if not points:
            raise ValueError("Points are empty.")
        
        qdrant_points = []
        
        for point in points :
            qdrant_points.append(
                models.PointStruct(
                    id = point.point_id,
                    vector = point.vector,
                    payload = point.payload,
                )
            )

        self._client.upsert(
            collection_name=self._settings.qdrant_collection,
            points=qdrant_points,
        )

    def search(self, query_vector: list[float], top_k: int, allowed_access_levels: list[str]) -> list[VectorSearchResult]:
        # Bai tap Milestone 7:
        # 1. Validate query_vector khong rong.
        # 2. Validate top_k > 0.
        # 3. Tao filter accessLevel in allowed_access_levels.
        # 4. Goi Qdrant search/query_points.
        # 5. Map ket qua thanh list[VectorSearchResult].
        #
        # Luu y:
        # - filter permission phai nam o Qdrant payload de tranh leak document admin-level.
        
        if not query_vector :
            raise ValueError("Query vector are empty.")
        if top_k <= 0 : 
            raise ValueError("must be greater than 0.")
        if not allowed_access_levels:
            raise ValueError("allowed_access_levels is empty.")
        
        query_filter = models.Filter(
            must=[
                models.FieldCondition(
                    key="accessLevel",
                    match=models.MatchAny(any=allowed_access_levels),
                )
            ]
        )
        
        result = self._client.query_points(
            collection_name=self._settings.qdrant_collection,
            query=query_vector,
            query_filter=query_filter,
            limit=top_k,
            with_payload=True,
        )
                
        return [
            VectorSearchResult(
                score=point.score,
                payload=point.payload or {},
            )
            for point in result.points
        ]