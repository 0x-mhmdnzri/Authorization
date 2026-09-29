import { ModelListPage } from "@/components/ModelListPage";

export default function RadacPage() {
  return (
    <ModelListPage
      title="Risk Adaptive (RAdAC)"
      description="Risk thresholds and assessment logs."
      methods="RAdAC"
      endpoint="/api/radac/policies"
    />
  );
}
