namespace LabTrack.Data
{
    // Fotos de los equipos de demostración, alojadas en Cloudinary. Son públicas: verlas no
    // requiere credenciales.
    public static class FotosDemostracion
    {
        private static readonly IReadOnlyDictionary<string, string> Urls = new Dictionary<string, string>
        {
            ["AE-0033"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265547/labtrack/equipos/ynatpnmoz8ukdby9khcq.jpg",
            ["AN-0077"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265525/labtrack/equipos/woatm1ltkmnpxkl8vs18.jpg",
            ["AP-0030"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265536/labtrack/equipos/cjlon7mj3yp3cmgazqby.jpg",
            ["AR-0411"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265521/labtrack/equipos/abmufgbudqvhqhelipjg.jpg",
            ["CA-0118"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265526/labtrack/equipos/u24h6iw7aqcom8yitx0p.jpg",
            ["CR-0009"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265539/labtrack/equipos/qdvqeeq8nrhtlhqh6cus.jpg",
            ["FP-0190"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265516/labtrack/equipos/slknkrdbyq6vkhqlzpbh.jpg",
            ["FW-0003"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265537/labtrack/equipos/pdpaccitkjoneqvih1bm.jpg",
            ["GF-0215"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265524/labtrack/equipos/uohymzvjdbtxnzsaqrl9.jpg",
            ["LT-0219"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265518/labtrack/equipos/kvgcxhq9czbsetewy0mg.jpg",
            ["LT-0231"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265511/labtrack/equipos/jhifcbphvjbw39f9gztz.jpg",
            ["LT-0231-2"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265539/labtrack/equipos/kxrl5fsjki45a1utykue.jpg",
            ["LT-0244"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265512/labtrack/equipos/kshndibulsh1tkt4kkzy.jpg",
            ["LT-0260"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265519/labtrack/equipos/n1oif9gtk4ragyyfpf3n.jpg",
            ["MM-0308"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265514/labtrack/equipos/rrkkamoeiyds5nqfwgk9.jpg",
            ["MM-0308-2"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265543/labtrack/equipos/nrfrrahag4jmdesqmmjx.jpg",
            ["MM-0312"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265523/labtrack/equipos/dgleqi9hvzpkzmoezdgk.jpg",
            ["OS-0112"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265514/labtrack/equipos/q0dde3q16nwgkisrwwra.jpg",
            ["OS-0112-2"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265541/labtrack/equipos/wsrahttpobwzgdioiccq.jpg",
            ["PC-0101"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265520/labtrack/equipos/kgpwkyf1rmlpldl4ioqs.jpg",
            ["PR-0340"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265527/labtrack/equipos/pfszdrfvxo9l551mo1ke.jpg",
            ["PY-0007"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265545/labtrack/equipos/ekoslysytefix0dfivdw.jpg",
            ["RP-0520"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265513/labtrack/equipos/t204ytvtmrkhhwsu4ut5.jpg",
            ["RT-0045"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265517/labtrack/equipos/k4vswoalwbkjt8j4dune.jpg",
            ["RT-0045-2"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265543/labtrack/equipos/uaplompc319ibixmlg9p.jpg",
            ["RT-0052"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265527/labtrack/equipos/jbb2xouxj4y9fy3w6wpa.jpg",
            ["SW-0011"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265546/labtrack/equipos/cy7sshulvklajbtaxlsp.jpg",
            ["SW-0071"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265535/labtrack/equipos/taeuaclu6gwnrobnh2dd.jpg",
            ["SW-0078"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265518/labtrack/equipos/kfzjhlrmb0ly2ski2umo.jpg",
            ["SW-0078-2"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265544/labtrack/equipos/kgisbiuwv2a45j6ftfc2.jpg",
            ["TB-0087"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265522/labtrack/equipos/mjpo6hmorb2amkxgsjhw.jpg",
            ["TS-0012"] = "https://res.cloudinary.com/iyyr0pl5/image/upload/v1794265538/labtrack/equipos/rhl6sv9l5tm2emvzyjzi.jpg",
        };

        public static string? Para(string codigoInventario) => Urls.GetValueOrDefault(codigoInventario);

        public static bool EsDeDemostracion(string url) => Urls.Values.Contains(url);
    }
}
