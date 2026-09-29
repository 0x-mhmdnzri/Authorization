import { ModelListPage } from "@/components/ModelListPage";

export default function PurposePage() {
  return (
    <ModelListPage
      title="Purposes"
      description="Purpose-based access – access only for an allowed purpose code."
      methods="PBAC-Purpose"
      endpoint="/api/purpose/purposes"
    />
  );
}
