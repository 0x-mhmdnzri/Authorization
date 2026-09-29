import { ModelListPage } from "@/components/ModelListPage";

export default function PacPage() {
  return (
    <ModelListPage
      title="Privileged Access (PAC)"
      description="Just-in-time privilege definitions and elevation workflow."
      methods="PAC"
      endpoint="/api/pac/privileges"
    />
  );
}
