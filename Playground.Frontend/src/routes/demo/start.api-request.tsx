import { createFileRoute } from "@tanstack/react-router";
import { fetchWithAuth, enforceLogin } from "@/oidc";
import Spinner from "@/components/Spinner";

type EndpointResult = {
    open: string;
    restricted: string;
};

export const Route = createFileRoute("/demo/start/api-request")({
    beforeLoad: enforceLogin,
    loader: async (): Promise<EndpointResult> => {
        const [openRes, restrictedRes] = await Promise.all([
            fetch("/open-endpoint"),
            fetchWithAuth("/restricted-endpoint")
        ]);

        return {
            open: await openRes.text(),
            restricted: restrictedRes.ok
                ? await restrictedRes.text()
                : `${restrictedRes.status} ${restrictedRes.statusText}`
        };
    },
    pendingComponent: () => (
        <div className="flex flex-1 items-center justify-center py-16">
            <Spinner />
        </div>
    ),
    component: ApiRequestDemo
});

function ApiRequestDemo() {
    const { open, restricted } = Route.useLoaderData();

    return (
        <div className="flex flex-1 items-center justify-center min-h-full p-4 text-white">
            <div className="w-full max-w-2xl p-8 rounded-xl backdrop-blur-md bg-black/50 shadow-xl border-8 border-black/10 opacity-0 animate-[fadeIn_0.2s_ease-in_forwards]">
                <h1 className="text-2xl mb-6">Playground.Server DPoP Endpoints</h1>
                <div className="space-y-4">
                    <div className="bg-white/10 border border-white/20 rounded-lg p-4">
                        <p className="text-sm text-white/60 mb-1">GET /open-endpoint (no auth)</p>
                        <p className="text-white">{open}</p>
                    </div>
                    <div className="bg-white/10 border border-white/20 rounded-lg p-4">
                        <p className="text-sm text-white/60 mb-1">GET /restricted-endpoint (DPoP-bound token)</p>
                        <p className="text-white">{restricted}</p>
                    </div>
                </div>
            </div>
        </div>
    );
}
