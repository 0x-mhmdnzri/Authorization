import { ModelListPage } from "@/components/ModelListPage";

export default function CbacPage() {
  return (
    <ModelListPage
      title="Context Policies"
      description="Time, network, device, location and auth-strength context rules."
      methods="CBAC"
      endpoint="/api/cbac/policies"
    />
  );
}
