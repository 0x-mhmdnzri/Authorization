import { ModelListPage } from "@/components/ModelListPage";

export default function ResourcesPage() {
  return (
    <ModelListPage
      title="Resources & ACL"
      description="Resources used by DAC ownership and MAC classification."
      methods="DAC,MAC"
      endpoint="/api/abac/resources"
    />
  );
}
