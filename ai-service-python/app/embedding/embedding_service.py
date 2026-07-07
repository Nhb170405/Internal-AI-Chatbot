from openai import OpenAI

from app.config.settings import AppSettings


class EmbeddingService:
    def __init__(self, settings: AppSettings):
        # Bai tap Milestone 7:
        # 1. Luu settings vao self._settings.
        # 2. Tao OpenAI client bang api_key trong settings.
        # 3. Khong print/log API key.
        self._settings = settings
        self._client = OpenAI(api_key = settings.openai_api_key)
        
    def embed_text(self, text: str) -> list[float]:
        # Bai tap Milestone 7:
        # 1. Validate text khong rong.
        # 2. Goi OpenAI embeddings API voi model settings.openai_embedding_model.
        # 3. Lay vector tu response.data[0].embedding.
        # 4. Return list[float].
        #
        # Goi y OpenAI SDK:
        # response = self._client.embeddings.create(
        #     model=self._settings.openai_embedding_model,
        #     input=text,
        # )
        # return response.data[0].embedding
        cleaned = text.strip()
        if not cleaned:
            raise ValueError("Text is empty.")

        response = self._client.embeddings.create(
            model = self._settings.openai_embedding_model,
            input = cleaned,
        )
        
        return response.data[0].embedding



    def embed_texts(self, texts: list[str]) -> list[list[float]]:
        # Bai tap Milestone 7:
        # 1. Validate list khong rong.
        # 2. Validate khong co item rong.
        # 3. Goi embeddings API mot lan voi input=texts de tiet kiem HTTP calls.
        # 4. Return list embeddings cung thu tu voi texts.
        #
        # Goi y:
        # response.data la danh sach item co .index va .embedding.
        # Ban dau co the return theo thu tu response.data neu OpenAI tra dung thu tu.
        
        if not texts:
            raise ValueError("Text is empty.")
        
        cleaned_texts = []

        for text in texts:
            cleaned = text.strip()
            if not cleaned:
                raise ValueError("Text item is empty.")
            cleaned_texts.append(cleaned)
            
        response = self._client.embeddings.create(
            model = self._settings.openai_embedding_model,
            input = cleaned_texts,
        )
        
        embeddings = [item.embedding for item in response.data]

        return embeddings