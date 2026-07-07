type LoadingScreenProps = {
  message: string;
};

export function LoadingScreen({ message }: LoadingScreenProps) {
  return (
    <main className="center-screen">
      <div className="loading-dot" />
      <p>{message}</p>
    </main>
  );
}
