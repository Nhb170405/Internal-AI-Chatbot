type TextInputProps = {
  label: string;
  value: string;
  type?: "text" | "password" | "email";
  placeholder?: string;
  onChange: (value: string) => void;
};

export function TextInput({ label, value, type = "text", placeholder, onChange }: TextInputProps) {
  return (
    <label className="field">
      <span>{label}</span>
      <input
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}
