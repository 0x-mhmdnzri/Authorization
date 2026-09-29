import { ModelListPage } from "@/components/ModelListPage";

export default function PbacPage() {
  return (
    <ModelListPage
      title="Central Policies (PBAC)"
      description="Policy Decision Point – centralized policies combining roles, attributes, DAC."
      methods="PBAC-Policy"
      endpoint="/api/pbac/policies"
    />
  );
}
