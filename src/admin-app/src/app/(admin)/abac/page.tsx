import { ModelListPage } from "@/components/ModelListPage";

export default function AbacPage() {
  return (
    <ModelListPage
      title="ABAC Policies"
      description="Attribute-based policies evaluated over subject, resource, action, environment."
      methods="ABAC"
      endpoint="/api/abac/policies"
    />
  );
}
