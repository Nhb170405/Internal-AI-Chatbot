export type ChartType = "bar" | "line" | "pie";

export type ChartRequest = {
  chartType: ChartType;
  operation: string;
  sheetName?: string;
  valueColumn?: string;
  groupByColumn?: string;
  topN?: number;
  title?: string;
  xField?: string;
  yField?: string;
};

export type ChartResponse = {
  documentId: string;
  success: boolean;
  chartType: ChartType;
  chartPath: string | null;
  chartUrl: string | null;
  data: unknown;
  warnings: string[];
  errorMessage: string | null;
};
