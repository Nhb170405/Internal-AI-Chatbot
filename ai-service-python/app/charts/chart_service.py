from app.charts.chart_models import ChartRenderRequest, ChartRenderResponse
from pathlib import Path

from uuid import uuid4
import matplotlib.pyplot as plt

def render_chart(request: ChartRenderRequest) -> ChartRenderResponse:
    # Bai tap Milestone 12:
    # 1. Validate request.data khong rong.
    # 2. Normalize chartType ve lowercase.
    # 3. Chi chap nhan "bar", "line", "pie" trong version dau.
    # 4. Chon xField/yField:
    #    - Neu request co xField/yField thi dung chung.
    #    - Neu khong co thi suy luan tu 2 key dau tien cua data row dau tien.
    # 5. Lay danh sach x_values va y_values tu request.data.
    # 6. Validate y_values la numeric.
    # 7. Dung matplotlib de ve:
    #    - bar: plt.bar(x_values, y_values)
    #    - line: plt.plot(x_values, y_values, marker="o")
    #    - pie: plt.pie(y_values, labels=x_values)
    # 8. Tao thu muc generated/charts neu chua co.
    # 9. Luu file PNG voi ten random an toan.
    # 10. Return ChartRenderResponse success=true kem chartPath va data goc.
    #
    # Luu y thiet ke:
    # - Khong doc CSV/XLSX trong file nay.
    # - Khong goi pandas group_by/sum/top_n trong file nay.
    # - Neu can thay doi logic tinh toan, sua DatasetAnalysisService truoc.
    try: 
        if not request.data:
                raise ValueError("Chart data is empty.")
        
        chart_type = _normalize_chart_type(request.chartType)
        x_field, y_field = _resolve_fields(request.data, request.xField, request.yField)
        
        x_values, y_values = _extract_xy_values(request.data, x_field, y_field)
        
        if chart_type == "bar":
            plt.bar(x_values, y_values)
            plt.xlabel(x_field)
            plt.ylabel(y_field)
            plt.xticks(rotation=30, ha="right")

        elif chart_type == "line":
            plt.plot(x_values, y_values, marker="o")
            plt.xlabel(x_field)
            plt.ylabel(y_field)
            plt.xticks(rotation=30, ha="right")

        elif chart_type == "pie":
            if sum(y_values) <= 0:
                raise ValueError("Pie chart requires positive numeric values.")

            plt.pie(y_values, labels=x_values, autopct="%1.1f%%")
            plt.axis("equal")
                            
        if request.title:
            plt.title(request.title)

        plt.tight_layout()

        chart_path = _save_chart_file()
        
        return ChartRenderResponse(
            success=True,
            chartType=chart_type,
            chartPath=chart_path,
            data=request.data,
            warnings=[],
            errorMessage=None
        )
        
    except Exception as e:
        return ChartRenderResponse(
            success=False,
            chartType=request.chartType,
            chartPath=None,
            data=request.data,
            warnings=[],
            errorMessage=str(e)
        )
        
    finally:
        plt.close()
        
        
    """
    Helper
    """
    
def _normalize_chart_type(chart_type: str) -> str:
    normalized = chart_type.strip().lower()

    if normalized not in ["bar", "line", "pie"]:
        raise ValueError("Chart type not supported.")

    return normalized
    
def _resolve_fields(data: list[dict], x_field: str | None, y_field: str | None):
    if not data:
        raise ValueError("Chart data is empty.")

    first_row = data[0]
    keys = list(first_row.keys())

    if len(keys) < 2:
        raise ValueError("Chart data must contain at least two fields.")

    if x_field is None or x_field.strip() == "":
        resolved_x = keys[0]
    else:
        resolved_x = _find_field(keys, x_field)
        if resolved_x is None:
            raise ValueError(f"xField '{x_field}' does not exist in chart data.")

    if y_field is None or y_field.strip() == "":
        resolved_y = keys[1]
    else:
        resolved_y = _find_field(keys, y_field)
        if resolved_y is None:
            raise ValueError(f"yField '{y_field}' does not exist in chart data.")

    return resolved_x, resolved_y

def _extract_xy_values(data, x_field, y_field):
    x_values = []
    y_values = []
    
    for row in data:
        if x_field not in row:
            raise ValueError(f"xField '{x_field}' is missing in one or more rows.")

        if y_field not in row:
            raise ValueError(f"yField '{y_field}' is missing in one or more rows.")
        
        x_value = row[x_field]
        y_raw = row[y_field]
        
        if y_raw is None:
            raise ValueError(f"yField '{y_field}' contains null value.")
        
        try:
            y_value = float(y_raw)
        except (TypeError, ValueError):
            raise ValueError(f"yField '{y_field}' must contain numeric values.")

        x_values.append(str(x_value))
        y_values.append(y_value)

    if not x_values:
        raise ValueError("Chart data has no rows.")

    return x_values, y_values

def _ensure_chart_output_dir() -> Path:
    project_root = Path(__file__).resolve().parents[2]
    output_dir = project_root / "generated" / "charts"
    output_dir.mkdir(parents=True, exist_ok=True)
    return output_dir

def _save_chart_file() -> str:
    output_dir = _ensure_chart_output_dir()

    file_name = f"chart_{uuid4().hex}.png"
    file_path = output_dir / file_name

    plt.savefig(file_path, format="png", dpi=150)

    return str(file_path)

def _find_field(keys: list[str], requested_field: str | None):
    if requested_field is None or requested_field.strip() == "":
        return None

    normalized_request = requested_field.strip().lower()

    for key in keys:
        normalized_key = str(key).strip().lower()

        if normalized_key == normalized_request:
            return key

    return None
