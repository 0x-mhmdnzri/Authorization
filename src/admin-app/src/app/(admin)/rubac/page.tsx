import { ModelListPage } from "@/components/ModelListPage";

export default function RubacPage() {
  return (
    <ModelListPage
      title="Access Rules (RuBAC)"
      description="If-then rules: IP, time, department, role, rate limits."
      methods="RuBAC"
      endpoint="/api/rubac/rules"
    />
  );
}
